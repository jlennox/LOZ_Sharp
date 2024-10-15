using System.Reflection;
using ImGuiNET;
using z1.IO;
using z1.UI;

namespace z1.GUI;

internal static class GLWindowGui
{
    private static PropertyInfo GetProperty<T>(string name) => GetProperty<T, bool>(name);

    private static PropertyInfo GetProperty<T, TPropType>(string name)
    {
        var property = typeof(T).GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new Exception($"Property {name} not found on {typeof(T).Name}");

        if (property.PropertyType != typeof(TPropType))
        {
            throw new Exception($"Property {name} on {typeof(T).Name} is not a {typeof(TPropType).Name}");
        }

        return property;
    }

    private static class GameEnhancementsProperties
    {
        private static PropertyInfo GetProperty(string name) => GetProperty<GameEnhancements>(name);

        public static readonly PropertyInfo AutoSave = GetProperty(nameof(GameEnhancements.AutoSave));
        public static readonly PropertyInfo ImprovedMenus = GetProperty(nameof(GameEnhancements.ImprovedMenus));
        public static readonly PropertyInfo ReduceFlashing = GetProperty(nameof(GameEnhancements.ReduceFlashing));
        public static readonly PropertyInfo RedCandleLightsDarkRooms = GetProperty(nameof(GameEnhancements.RedCandleLightsDarkRooms));
    }

    private static class AudioProperties
    {
        private static PropertyInfo GetProperty(string name) => GetProperty<AudioConfigurationPassthrough>(name);

        public static readonly PropertyInfo Mute = GetProperty(nameof(AudioConfigurationPassthrough.Mute));
        public static readonly PropertyInfo MuteMusic = GetProperty(nameof(AudioConfigurationPassthrough.MuteMusic));
    }

    private static class DebugInfoProperties
    {
        private static PropertyInfo GetProperty(string name) => GetProperty<DebugInfoConfiguration>(name);

        public static readonly PropertyInfo Enabled = GetProperty(nameof(DebugInfoConfiguration.Enabled));
        public static readonly PropertyInfo RoomId = GetProperty(nameof(DebugInfoConfiguration.RoomId));
        public static readonly PropertyInfo ActiveShots  = GetProperty(nameof(DebugInfoConfiguration.ActiveShots));
        public static readonly PropertyInfo RoomAttributes = GetProperty(nameof(DebugInfoConfiguration.RoomAttributes));
    }

    private readonly struct AudioConfigurationPassthrough
    {
        private readonly AudioConfiguration _config;
        private readonly Game _game;

        public AudioConfigurationPassthrough(Game game)
        {
            _config = game.Configuration.Audio;
            _game = game;
        }

        public bool Mute
        {
            get => _config.Mute;
            set
            {
                _config.Mute = value;
                _game.Sound.SetMute(value);
            }
        }
        public bool MuteMusic
        {
            get => _config.MuteMusic;
            set
            {
                _config.MuteMusic = value;
                _game.Sound.SetMuteSongs(value);
            }
        }
        public int Volume
        {
            get => _config.Volume;
            set
            {
                _config.Volume = value;
                _game.Sound.SetVolume(value);
            }
        }
    }

    public static void DrawMenu(GLWindow window)
    {
        var game = window.Game;

        static void DrawMenuItem(string name, PropertyInfo property, object target)
        {
            // Not the most efficient way to do it, but this is rarely rendered.
            if (ImGui.MenuItem(name, null, (bool)property.GetValue(target)))
            {
                property.SetValue(target, !(bool)property.GetValue(target));
                SaveFolder.SaveConfiguration();
            }
        }

        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("Save", game.World.Profile != null)) game.AutoSave();
                if (ImGui.MenuItem("Open save folder")) Directories.OpenSaveFolder();
                ImGui.Separator();
                if (ImGui.MenuItem("Exit Game")) Environment.Exit(0);
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Display"))
            {
                if (ImGui.MenuItem("Fullscreen", null, window.IsFullScreen)) window.ToggleFullscreen();
                if (ImGui.BeginMenu("Debug Info"))
                {
                    DrawMenuItem("Enabled", DebugInfoProperties.Enabled, game.Configuration.DebugInfo);
                    DrawMenuItem("Room Id", DebugInfoProperties.RoomId, game.Configuration.DebugInfo);
                    DrawMenuItem("Active Shots", DebugInfoProperties.ActiveShots, game.Configuration.DebugInfo);
                    DrawMenuItem("Room Attributes", DebugInfoProperties.RoomAttributes, game.Configuration.DebugInfo);
                    ImGui.EndMenu();
                }
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Audio"))
            {
                var config = new AudioConfigurationPassthrough(game);
                DrawMenuItem("Mute", AudioProperties.Mute, config);
                DrawMenuItem("Mute Music", AudioProperties.MuteMusic, config);
                var volume = config.Volume;
                if (ImGui.SliderInt("Volume", ref volume, 0, 100))
                {
                    config.Volume = volume;
                    SaveFolder.SaveConfiguration();
                }
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Debug"))
            {
                if (ImGui.MenuItem("Reset all"))
                {
                    foreach (var flags in game.World.Profile.RoomFlags)
                    {
                        flags.ShortcutState = false;
                        flags.ItemState = false;
                        flags.SecretState = false;
                        flags.ObjectCount = 0;
                    }
                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Enhancements"))
            {
                DrawMenuItem("AutoSave", GameEnhancementsProperties.AutoSave, game.Enhancements);
                DrawMenuItem("Red Candle Auto-Lights Darkrooms", GameEnhancementsProperties.RedCandleLightsDarkRooms, game.Enhancements);
                DrawMenuItem("Improved Menus", GameEnhancementsProperties.ImprovedMenus, game.Enhancements);
                DrawMenuItem("Reduce Flashing", GameEnhancementsProperties.ReduceFlashing, game.Enhancements);

                var speed = game.Enhancements.TextSpeed;
                if (ImGui.SliderInt("Text Speed", ref speed,
                        GameEnhancements.TextSpeedMin,
                        GameEnhancements.TextSpeedMax))
                {
                    game.Enhancements.TextSpeed = speed;
                    SaveFolder.SaveConfiguration();
                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Debug"))
            {
                if (ImGui.MenuItem("Clear history")) game.GameCheats.TriggerCheat<GameCheats.ClearHistoryCheat>();
                if (ImGui.MenuItem("Draw hit detection", null, game.World.DrawHitDetection)) game.World.DrawHitDetection = !game.World.DrawHitDetection;
                ImGui.EndMenu();
            }

            DrawWarpMenu(game);

            ImGui.EndMainMenuBar();
        }
    }

    private static void DrawWarpMenu(Game game)
    {
#if !DEBUG
        return;
#endif

        static void Warp(Game game, int levelNumber)
        {
            game.World.KillAllObjects();
            game.World.GotoLoadLevel(levelNumber);
        }

        static void WarpOW(Game game, int x, int y)
        {
            game.World.KillAllObjects();
            game.World.LoadOverworldRoom(x, y);
        }

        if (ImGui.BeginMenu("Warp"))
        {
            if (ImGui.MenuItem("Level 1")) Warp(game, 1);
            if (ImGui.MenuItem("Level 1 (Entrance)")) WarpOW(game, 7, 3);
            if (ImGui.MenuItem("Level 2")) Warp(game, 2);
            if (ImGui.MenuItem("Level 3")) Warp(game, 3);
            if (ImGui.MenuItem("Level 4")) Warp(game, 4);
            if (ImGui.MenuItem("Level 5")) Warp(game, 5);
            if (ImGui.MenuItem("Level 6")) Warp(game, 6);
            if (ImGui.MenuItem("Level 6 (Entrance)")) WarpOW(game, 2, 2);
            if (ImGui.MenuItem("Level 7")) Warp(game, 7);
            if (ImGui.MenuItem("Level 7 (Entrance)")) WarpOW(game, 2, 4);
            if (ImGui.MenuItem("Level 8")) Warp(game, 8);
            if (ImGui.MenuItem("Level 9")) Warp(game, 9);

            if (game.World.IsOverworld())
            {
                ImGui.Separator();
                if (ImGui.MenuItem("Raft")) WarpOW(game, 5, 5);
                if (ImGui.MenuItem("Ghost")) WarpOW(game, 1, 2);
                if (ImGui.MenuItem("Armos / Bracelet")) WarpOW(game, 4, 2);
                if (ImGui.MenuItem("Ladder / Heart")) WarpOW(game, 15, 5);
                if (ImGui.MenuItem("Cave 12: Lost hills hint")) WarpOW(game, 0, 7);
                if (ImGui.MenuItem("Cave 15: Shop")) WarpOW(game, 6, 6);
            }

            ImGui.EndMenu();
        }
    }
}