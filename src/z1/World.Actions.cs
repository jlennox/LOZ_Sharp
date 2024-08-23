﻿using System.Collections.Immutable;

namespace z1;

internal partial class World
{
    private static readonly ImmutableArray<byte> _levelGroups = [0, 0, 1, 1, 0, 1, 0, 1, 2];

    private readonly record struct EquipValue(byte Value, ItemSlot Slot);

    // The item ID to item slot map is at $6B14, and copied to RAM at $72A4.
    // The item ID to item value map is at $6B38, and copied to RAM at $72C8.
    // They're combined here.
    private static readonly ImmutableArray<EquipValue> _itemToEquipment = [
        new EquipValue(4, ItemSlot.Bombs),
        new EquipValue(1, ItemSlot.Sword),
        new EquipValue(2, ItemSlot.Sword),
        new EquipValue(3, ItemSlot.Sword),
        new EquipValue(1, ItemSlot.Food),
        new EquipValue(1, ItemSlot.Recorder),
        new EquipValue(1, ItemSlot.Candle),
        new EquipValue(2, ItemSlot.Candle),
        new EquipValue(1, ItemSlot.Arrow),
        new EquipValue(2, ItemSlot.Arrow),
        new EquipValue(1, ItemSlot.Bow),
        new EquipValue(1, ItemSlot.MagicKey),
        new EquipValue(1, ItemSlot.Raft),
        new EquipValue(1, ItemSlot.Ladder),
        new EquipValue(1, ItemSlot.PowerTriforce),
        new EquipValue(5, ItemSlot.RupeesToAdd),
        new EquipValue(1, ItemSlot.Rod),
        new EquipValue(1, ItemSlot.Book),
        new EquipValue(1, ItemSlot.Ring),
        new EquipValue(2, ItemSlot.Ring),
        new EquipValue(1, ItemSlot.Bracelet),
        new EquipValue(1, ItemSlot.Letter),
        new EquipValue(1, ItemSlot.Compass9),
        new EquipValue(1, ItemSlot.Map9),
        new EquipValue(1, ItemSlot.RupeesToAdd),
        new EquipValue(1, ItemSlot.Keys),
        new EquipValue(1, ItemSlot.HeartContainers),
        new EquipValue(1, ItemSlot.TriforcePieces),
        new EquipValue(1, ItemSlot.MagicShield),
        new EquipValue(1, ItemSlot.Boomerang),
        new EquipValue(2, ItemSlot.Boomerang),
        new EquipValue(1, ItemSlot.Potion),
        new EquipValue(2, ItemSlot.Potion),
        new EquipValue(1, ItemSlot.Clock),
        new EquipValue(1, ItemSlot.Bombs),
        new EquipValue(3, ItemSlot.Bombs)
    ];

    private readonly record struct DoorStateBehaviors(TileBehavior Closed, TileBehavior Open);

    private static readonly ImmutableArray<DoorStateBehaviors> _doorBehaviors = [
        new DoorStateBehaviors(TileBehavior.Doorway, TileBehavior.Doorway),     // Open
        new DoorStateBehaviors(TileBehavior.Wall, TileBehavior.Wall),           // Wall (None)
        new DoorStateBehaviors(TileBehavior.Door, TileBehavior.Door),           // False Wall
        new DoorStateBehaviors(TileBehavior.Door, TileBehavior.Door),           // False Wall 2
        new DoorStateBehaviors(TileBehavior.Door, TileBehavior.Door),           // Bombable
        new DoorStateBehaviors(TileBehavior.Door, TileBehavior.Doorway),        // Key
        new DoorStateBehaviors(TileBehavior.Door, TileBehavior.Doorway),        // Key 2
        new DoorStateBehaviors(TileBehavior.Door, TileBehavior.Doorway)         // Shutter
    ];

    private static readonly ImmutableArray<Point> _doorMiddles = [
        new Point(0xE0, 0x98),
        new Point(0x20, 0x98),
        new Point(0x80, 0xD0),
        new Point(0x80, 0x60)
    ];

    private static readonly ImmutableArray<int> _doorSrcYs = [64, 96, 0, 32];

    private static readonly ImmutableArray<Point> _doorPos = [
        new Point(224, 136),
        new Point(0,   136),
        new Point(112, 208),
        new Point(112, 64)
    ];

    private readonly record struct DoorStateFaces(byte Closed, byte Open);

    private static readonly ImmutableArray<DoorStateFaces> _doorFaces = [
        new DoorStateFaces(0, 0),
        new DoorStateFaces(3, 3),
        new DoorStateFaces(3, 3),
        new DoorStateFaces(3, 3),
        new DoorStateFaces(3, 4),
        new DoorStateFaces(1, 0),
        new DoorStateFaces(1, 0),
        new DoorStateFaces(2, 0)
    ];

    private static readonly ImmutableArray<Cell> _doorCorners = [
        new Cell(0x0A, 0x1C),
        new Cell(0x0A, 0x02),
        new Cell(0x12, 0x0F),
        new Cell(0x02, 0x0F)
    ];

    private static readonly ImmutableArray<Cell> _behindDoorCorners = [
        new Cell(0x0A, 0x1E),
        new Cell(0x0A, 0x00),
        new Cell(0x14, 0x0F),
        new Cell(0x00, 0x0F)
    ];

    private delegate void TileActionDel(int row, int col, TileInteraction interaction);

    private ImmutableArray<TileActionDel> ActionFuncs => [
        NoneTileAction,
        PushTileAction,
        BombTileAction,
        BurnTileAction,
        HeadstoneTileAction,
        LadderTileAction,
        RaftTileAction,
        CaveTileAction,
        StairsTileAction,
        GhostTileAction,
        ArmosTileAction,
        BlockTileAction
    ];

    private ImmutableArray<TileActionDel> BehaviorFuncs => [
        NoneTileAction,
        NoneTileAction,
        NoneTileAction,
        StairsTileAction,
        NoneTileAction,

        NoneTileAction,
        NoneTileAction,
        CaveTileAction,
        GhostTileAction,
        GhostTileAction,
        GhostTileAction,
        GhostTileAction,
        GhostTileAction,
        GhostTileAction,
        GhostTileAction,
        GhostTileAction,
        GhostTileAction,
        GhostTileAction,
        GhostTileAction,
        GhostTileAction,
        GhostTileAction,
        GhostTileAction,
        GhostTileAction,
        GhostTileAction,
        ArmosTileAction,
        ArmosTileAction,
        ArmosTileAction,
        ArmosTileAction,
        ArmosTileAction,
        ArmosTileAction,
        ArmosTileAction,
        ArmosTileAction,
        ArmosTileAction,
        ArmosTileAction,
        ArmosTileAction,
        ArmosTileAction,
        ArmosTileAction,
        ArmosTileAction,
        ArmosTileAction,
        ArmosTileAction,
        DoorTileAction,
        NoneTileAction
    ];

    private ImmutableArray<Action?> ModeFuncs => [
        null,
        UpdateGameMenu,
        UpdateLoadLevel,
        UpdateUnfurl,
        UpdateEnter,
        UpdatePlay,
        UpdateLeave,
        UpdateScroll,
        UpdateContinueQuestion,
        UpdatePlay,
        UpdateLeaveCellar,
        UpdatePlay,
        null,
        null,
        UpdateRegisterMenu,
        UpdateEliminateMenu,
        UpdateStairsState,
        UpdateDie,
        UpdateEndLevel,
        UpdateWinGame,
        UpdatePlayCellar,
        UpdatePlayCave
    ];

    private Action? GetDrawFuncs(GameMode mode) => mode switch
    {
        GameMode.Demo => null,
        GameMode.GameMenu => DrawGameMenu,
        GameMode.LoadLevel => DrawLoadLevel,
        GameMode.Unfurl => DrawUnfurl,
        GameMode.Enter => DrawEnter,
        GameMode.Play => DrawPlay,
        GameMode.Leave => DrawLeave,
        GameMode.Scroll => DrawScroll,
        GameMode.ContinueQuestion => DrawContinueQuestion,
        GameMode.PlayCellar => DrawPlay,
        GameMode.LeaveCellar => DrawLeaveCellar,
        GameMode.PlayCave => DrawPlay,
        GameMode.PlayShortcuts => null,
        GameMode.UnknownD__ => null,
        GameMode.Register => DrawGameMenu,
        GameMode.Elimination => DrawGameMenu,
        GameMode.Stairs => DrawStairsState,
        GameMode.Death => DrawDie,
        GameMode.EndLevel => DrawEndLevel,
        GameMode.WinGame => DrawWinGame,
        GameMode.InitPlayCellar => DrawPlayCellar,
        GameMode.InitPlayCave => DrawPlayCave,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Invalid game mode.")
    };

    private ImmutableArray<Action> PlayCellarFuncs => [
        UpdatePlayCellar_Start,
        UpdatePlayCellar_FadeOut,
        UpdatePlayCellar_LoadRoom,
        UpdatePlayCellar_FadeIn,
        UpdatePlayCellar_Walk
    ];

    private ImmutableArray<Action> PlayCaveFuncs => [
        UpdatePlayCave_Start,
        UpdatePlayCave_Wait,
        UpdatePlayCave_LoadRoom,
        UpdatePlayCave_Walk
    ];

    private ImmutableArray<Action> EndLevelFuncs => [
        UpdateEndLevel_Start,
        UpdateEndLevel_Wait,
        UpdateEndLevel_Flash,
        UpdateEndLevel_FillHearts,
        UpdateEndLevel_Wait,
        UpdateEndLevel_Furl,
        UpdateEndLevel_Wait
    ];

    private ImmutableArray<Action> WinGameFuncs => [
        UpdateWinGame_Start,
        UpdateWinGame_Text1,
        UpdateWinGame_Stand,
        UpdateWinGame_Hold1,
        UpdateWinGame_Colors,
        UpdateWinGame_Hold2,
        UpdateWinGame_Text2,
        UpdateWinGame_Hold3,
        UpdateWinGame_NoObjects,
        UpdateWinGame_Credits
    ];

    private ImmutableArray<Action> ScrollFuncs => [
        UpdateScroll_Start,
        UpdateScroll_AnimatingColors,
        UpdateScroll_FadeOut,
        UpdateScroll_LoadRoom,
        UpdateScroll_Scroll
    ];

    private ImmutableArray<Action> DeathFuncs => [
        UpdateDie_Start,
        UpdateDie_Flash,
        UpdateDie_Wait1,
        UpdateDie_Turn,
        UpdateDie_Fade,
        UpdateDie_GrayLink,
        UpdateDie_Spark,
        UpdateDie_Wait2,
        UpdateDie_GameOver
    ];

    private ImmutableArray<Action> LeaveCellarFuncs => [
        UpdateLeaveCellar_Start,
        UpdateLeaveCellar_FadeOut,
        UpdateLeaveCellar_LoadRoom,
        UpdateLeaveCellar_FadeIn,
        UpdateLeaveCellar_Walk,
        UpdateLeaveCellar_Wait,
        UpdateLeaveCellar_LoadOverworldRoom
    ];

    private ImmutableArray<Action> EnterFuncs => [
        UpdateEnter_Start,
        UpdateEnter_Wait,
        UpdateEnter_FadeIn,
        UpdateEnter_Walk,
        UpdateEnter_WalkCave
    ];
}
