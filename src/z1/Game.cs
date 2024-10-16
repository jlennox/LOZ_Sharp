﻿using System.Runtime.CompilerServices;
using z1.Actors;
using z1.IO;
using z1.UI;

namespace z1;

internal sealed class Game
{
    private static readonly DebugLog _log = new(nameof(Game));

    public Link Link;

    public readonly Sound Sound;

    public static class Cheats
    {
        public static bool SpeedUp = false;
        public static bool GodMode = false;
        public static bool NoClip = false;
    }

    public GameEnhancements Enhancements => Configuration.Enhancements;

    public World World;
    public Input Input;
    public GameCheats GameCheats;
    public GameConfiguration Configuration = SaveFolder.Configuration;
    public readonly OnScreenDisplay OnScreenDisplay = new();
    public readonly DebugInfo DebugInfo;

    public int FrameCounter { get; private set; }

    public Game()
    {
        World = new World(this);
        Input = new Input(Configuration.Input);
        GameCheats = new GameCheats(this, Input);
        Sound = new Sound(Configuration.Audio);
        DebugInfo = new DebugInfo(this, Configuration.DebugInfo);
    }

    public void Update()
    {
        ++FrameCounter;

        GameUpdate();
        World.Update();
        Sound.Update();
        GameCheats.Update();

        // Input comes last because it marks the buttons as old. We read them on a callback which happens async.
        Input.Update();
    }

    private void GameUpdate()
    {
        if (Input.IsButtonPressing(GameButton.AudioDecreaseVolume))
        {
            var volume = Sound.DecreaseVolume();
            Toast($"Volume:{volume}%");
        }

        if (Input.IsButtonPressing(GameButton.AudioIncreaseVolume))
        {
            var volume = Sound.IncreaseVolume();
            Toast($"Volume:{volume}%");
        }

        if (Input.IsButtonPressing(GameButton.AudioMuteToggle))
        {
            var isMuted = Sound.ToggleMute();
            Toast(isMuted ? "Sound muted" : "Sound unmuted");
        }

        if (Input.IsButtonPressing(GameButton.ToggleDebugInfo))
        {
            Configuration.DebugInfo.Enabled = !Configuration.DebugInfo.Enabled;
        }
    }

    public void Draw()
    {
        World.Draw();
        OnScreenDisplay.Draw();
        DebugInfo.Draw();
    }

    // JOE: TODO: Move to TryShoot pattern?
    public ObjectSlot ShootFireball(ObjType type, int x, int y)
    {
        if (World.TryFindEmptyMonsterSlot(out var slot))
        {
            var fireball = new FireballProjectile(this, type, x + 4, y, 1.75f);
            World.SetObject(slot, fireball);
            return slot;
        }

        return ObjectSlot.NoneFound;
    }

    public void AutoSave(bool announce = true, [CallerMemberName] string functionName = "")
    {
        _log.Write("AutoSave", $"Saving from {functionName}");

        if (announce) Toast("Auto-saving...");

        SaveFolder.SaveProfiles();
    }

    public void Toast(string text) => OnScreenDisplay.Toast(text);
}
