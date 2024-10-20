﻿using System.Collections.Immutable;
using z1.Render;
using z1.IO;

namespace z1.Actors;

internal abstract class BlueWizzrobeBase : WizzrobeBase
{
    public static readonly ImmutableArray<AnimationId> WizzrobeAnimMap = [
        AnimationId.UW_Wizzrobe_Right,
        AnimationId.UW_Wizzrobe_Left,
        AnimationId.UW_Wizzrobe_Right,
        AnimationId.UW_Wizzrobe_Up
    ];

    private static readonly DebugLog _log = new(nameof(BlueWizzrobeBase));

    protected byte FlashTimer; // ObjRemDistance
    protected byte TurnTimer;

    protected BlueWizzrobeBase(Game game, ObjType type, int x, int y)
        : base(game, type, x, y)
    {
        Decoration = 0;
        ObjTimer = 0;

        // Facing is required to be set, but ObjTimer = 0 causes `Turn` to be called first.
        // JOE: TODO: Add unit tests for this.
    }

    // BlueWizzrobe_AlignWithNearestSquare
    private void TruncatePosition()
    {
        X = (X + 8) & 0xF0;
        Y = (Y + 8) & 0xF0;
        Y -= 3;
    }

    // BlueWizzrobe_WalkOrTeleport
    protected void MoveOrTeleport()
    {
        if (ObjTimer != 0)
        {
            if (ObjTimer >= 0x10)
            {
                if ((Game.FrameCounter & 1) == 1)
                {
                    TurnIfNeeded();
                }
                else
                {
                    // BlueWizzrobe_TurnSometimesAndMoveAndCheckTile
                    TurnTimer++;
                    TurnIfNeeded();
                    MoveAndCollide();
                }
            }
            else if (ObjTimer == 1)
            {
                TryTeleporting();
            }

            return;
        }

        // BlueWizzrobe_AlignWithNearestSquareAndRandomizeTimer
        if (FlashTimer == 0)
        {
            var r = Random.Shared.GetByte();
            ObjTimer = (byte)(r | 0x70);
            TruncatePosition();
            Turn();
            return;
        }

        FlashTimer--;
        MoveAndCollide();
    }

    // BlueWizzrobe_TurnSometimesAndMoveAndCheckTile is this with a:
    //   - TurnTimer++
    //   - TurnIfNeeded();
    //   - MoveAndCollide();

    // BlueWizzrobe_MoveAndCheckTile
    protected void MoveAndCollide()
    {
        Move();

        var collisionResult = CheckWizzrobeTileCollision(X, Y, Facing);

        switch (collisionResult)
        {
            case WizzrobeTileCollisionResult.WallCollision:
                if (Facing.IsVertical()) Facing ^= Direction.VerticalMask;
                if (Facing.IsHorizontal()) Facing ^= Direction.HorizontalMask;

                Move();
                break;

            case WizzrobeTileCollisionResult.OtherCollision:
                if (FlashTimer == 0)
                {
                    // BeginTeleporting
                    FlashTimer = 0x20;
                    TurnTimer ^= 0x40;
                    ObjTimer = 0;
                    TruncatePosition();
                }

                break;
        }
    }

    // BlueWizzrobe_Move
    private void Move()
    {
        ReadOnlySpan<int> blueWizzrobeXSpeeds = [0, 1, -1, 0, 0, 1, -1, 0, 0, 1, -1];
        ReadOnlySpan<int> blueWizzrobeYSpeeds = [0, 0, 0, 0, 1, 1, 1, 0, -1, -1, -1];

        X += blueWizzrobeXSpeeds[(int)Facing];
        Y += blueWizzrobeYSpeeds[(int)Facing];
    }

    protected void TryShooting()
    {
        if (Game.World.HasItem(ItemSlot.Clock)) return;
        if (FlashTimer != 0) return;
        if ((Game.FrameCounter % 0x20) != 0) return;

        var player = Game.Link;
        Direction dir;

        if ((player.Y & 0xF0) != (Y & 0xF0))
        {
            if (player.X != (X & 0xF0)) return;
            dir = GetYDirToTruePlayer(Y);
        }
        else
        {
            dir = GetXDirToTruePlayer(X);
        }

        if (dir != Facing) return;

        Game.Sound.PlayEffect(SoundEffect.MagicWave);
        Shoot(ObjType.MagicWave, X, Y, Facing);
    }

    // L_BlueWizzrobe_TurnTowardLinkIfNeeded
    protected void TurnIfNeeded()
    {
        if ((TurnTimer & 0x3F) == 0)
        {
            Turn();
        }
    }

    // BlueWizzrobe_TurnTowardLink
    private void Turn()
    {
        var dir = (TurnTimer & 0x40) != 0
            ? GetYDirToTruePlayer(Y)
            : GetXDirToTruePlayer(X);

        if (dir == Facing) return;

        Facing = dir;
        TruncatePosition();
    }

    private static readonly ImmutableArray<int> _blueWizzrobeTeleportXOffsets = [-0x20, 0x20, -0x20, 0x20];
    private static readonly ImmutableArray<int> _blueWizzrobeTeleportYOffsets = [-0x20, -0x20, 0x20, 0x20];
    private static readonly ImmutableArray<int> _blueWizzrobeTeleportDirs = [0x0A, 9, 6, 5];

    // BlueWizzrobe_ChooseTeleportTarget
    private void TryTeleporting()
    {
        var index = Random.Shared.Next(4);

        var teleportX = X + _blueWizzrobeTeleportXOffsets[index];
        var teleportY = Y + _blueWizzrobeTeleportYOffsets[index];
        var dir = (Direction)_blueWizzrobeTeleportDirs[index];

        var collisionResult = CheckWizzrobeTileCollision(teleportX, teleportY, dir);
        if (collisionResult == WizzrobeTileCollisionResult.NoCollision)
        {
            Facing = dir;

            FlashTimer = 0x20;
            TurnTimer ^= 0x40;
            ObjTimer = 0;
        }
        else
        {
            var r = Random.Shared.GetByte();
            ObjTimer = (byte)(r | 0x70);
        }

        TruncatePosition();
    }

    internal enum WizzrobeTileCollisionResult
    {
        NoCollision = 0,
        WallCollision = 1,
        OtherCollision = 2
    }

    // Wizzrobe_GetCollidableTile implies dir=Facing.
    // Wizzrobe_GetCollidableTileForDir
    protected WizzrobeTileCollisionResult CheckWizzrobeTileCollision(int x, int y, Direction dir)
    {
        ReadOnlySpan<int> allWizzrobeCollisionXOffsets = [0xF0, 0x0F, 0, 0, 4, 8, 0, 0, 4, 8, 0];
        ReadOnlySpan<int> allWizzrobeCollisionYOffsets = [0x9F, 4, 4, 0, 8, 8, 8, 0, -8, 0, 0];

        var fnlog = _log.CreateFunctionLog();

        if (dir == Direction.None && this is not GanonActor)
        {
            throw new Exception($"{ObjType} at {Game.World.CurObjectSlot} attempted to CheckWizzrobeTileCollision with no direction.");
        }

        // JOE: NOTE: This is a deviation from the original game and C++ code.
        // In both it was "var ord = dir - 1;" and this would cause Ganon's index to underflow and read from index
        // 0xFF because their Facing isn't set beyond Direction.None before the initial calls to this function.
        // To keep with the original game's behavior, instead index 0 is initialized to the values found in the PRG0 ROM at those indexes.

        var ord = dir;
        x += allWizzrobeCollisionXOffsets[(int)ord];
        y += allWizzrobeCollisionYOffsets[(int)ord];

        var collision = Game.World.CollidesWithTileStill(x, y);
        fnlog.Write($"{Game.World.CurObjectSlot} FlashTimer:{FlashTimer} {dir} {x:X2},{y:X2} collision:({collision})");
        if (!collision.Collides) return WizzrobeTileCollisionResult.NoCollision;

        // This isn't quite the same as the original game, because the original contrasted
        // blocks and water together with everything else.
        return World.CollidesWall(collision.TileBehavior)
            ? WizzrobeTileCollisionResult.WallCollision
            : WizzrobeTileCollisionResult.OtherCollision;
    }
}

internal abstract class WizzrobeBase : Actor
{
    protected WizzrobeBase(Game game, ObjType type, int x, int y)
        : base(game, type, x, y) { }

    protected void CheckWizzrobeCollisions()
    {
        InvincibilityMask = 0xF6;
        if (InvincibilityTimer == 0)
        {
            CheckWave(ObjectSlot.PlayerSwordShot);
            CheckBombAndFire(ObjectSlot.Bomb);
            CheckBombAndFire(ObjectSlot.Bomb2);
            CheckBombAndFire(ObjectSlot.Fire);
            CheckBombAndFire(ObjectSlot.Fire2);
            CheckSword(ObjectSlot.PlayerSword, false);
        }
        CheckPlayerCollision();
    }
}

internal sealed class BlueWizzrobeActor : BlueWizzrobeBase
{
    private readonly SpriteAnimator _animator;

    public BlueWizzrobeActor(Game game, int x, int y)
        : base(game, ObjType.BlueWizzrobe, x, y)
    {
        _animator = new SpriteAnimator
        {
            DurationFrames = 16,
            Time = 0
        };
    }

    public override void Update()
    {
        if (Game.World.HasItem(ItemSlot.Clock))
        {
            // Force them to draw.
            FlashTimer = 0;
            AnimateAndCheckCollisions();
            return;
        }

        MoveOrTeleport();
        TryShooting();

        if ((FlashTimer & 1) == 0)
        {
            AnimateAndCheckCollisions();
        }

        SetFacingAnimation();
    }

    public override void Draw()
    {
        if ((FlashTimer & 1) == 0 && Facing != Direction.None)
        {
            var pal = CalcPalette(Palette.Blue);
            _animator.Draw(TileSheet.Npcs, X, Y, pal);
        }
    }

    private void SetFacingAnimation()
    {
        var dirOrd = Facing.GetOrdinal();
        _animator.Animation = Graphics.GetAnimation(TileSheet.Npcs, WizzrobeAnimMap[dirOrd]);
    }

    private void AnimateAndCheckCollisions()
    {
        _animator.Advance();
        CheckWizzrobeCollisions();
    }
}
