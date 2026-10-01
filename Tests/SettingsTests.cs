using Godot;
using System;
namespace Abyss.Tests;
internal sealed partial class RegressionSuite
{
    internal void TestSettingsMenu()
    {
        static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
        var host = new TestHost();
        var display = new MemoryDisplaySettings();
        var language = new TestSettings();
        var audio = new RecordingAudio();
        var test = new GameSession(host, new TestCanvas(), language, null, audio, display);
        test.GameInput.HandleKey(Key.Key2); test.GameInput.HandleKey(Key.Enter);
        Check(test.RunState.Screen == "settings", "Main settings are inaccessible.");
        test.GameInput.HandleKey(Key.D);
        Check(test.MenuState.English && language.LoadEnglish(LanguagePreferences.SettingsPath) == true, "Inline language was not saved.");
        test.GameInput.HandleKey(Key.Down); test.GameInput.HandleKey(Key.D);
        Check(test.MenuState.MusicVolume == 55 && audio.Music == 55 && audio.Effects == 75, "Music adjustment was not immediate or changed effects.");
        test.GameInput.HandleEvent(new InputEventKey { Keycode = Key.Right, Pressed = true, Echo = true });
        Check(test.MenuState.MusicVolume == 60, "Held volume key was ignored.");
        for (int i = 0; i < 30; i++) test.GameInput.HandleKey(Key.Left);
        Check(test.MenuState.MusicVolume == 0 && audio.Music == 0, "Music volume did not clamp at mute.");
        for (int i = 0; i < 30; i++) test.GameInput.HandleKey(Key.Right);
        Check(test.MenuState.MusicVolume == 100, "Music volume overflow.");
        test.GameInput.HandleKey(Key.S); test.GameInput.HandleKey(Key.A);
        Check(test.MenuState.SettingsIndex == 2 && test.MenuState.EffectsVolume == 70 && audio.Effects == 70, "Effects keyboard controls failed.");
        test.GameInput.HandleKey(Key.Down); test.GameInput.HandleKey(Key.Enter);
        Check(test.MenuState.Fullscreen && host.Display?.Fullscreen == true && display.Value?.Fullscreen == true, "Fullscreen was not applied and saved.");
        test.GameInput.HandleKey(Key.F11);
        Check(!test.MenuState.Fullscreen && host.Display?.Fullscreen == false, "F11 and settings state diverged.");
        test.GameInput.HandleKey(Key.Down);
        test.MenuState.ResolutionIndex = 0;
        for (int i = 0; i < SettingsController.Resolutions.Length; i++)
        {
            if (i > 0) test.GameInput.HandleKey(Key.D);
            else test.GameInput.HandleKey(Key.A);
            Check(host.Display?.Resolution == SettingsController.Resolutions[i] && display.Value?.Resolution == SettingsController.Resolutions[i], "Resolution was not applied and saved.");
        }
        test.GameInput.HandleKey(Key.D);
        Check(test.MenuState.ResolutionIndex == SettingsController.Resolutions.Length - 1, "Resolution selection overflow.");
        var restoredHost = new TestHost();
        var restored = new GameSession(restoredHost, new TestCanvas(), language, null, null, display);
        restored.SettingsController.LoadDisplay();
        Check(restoredHost.Display == display.Value && restored.MenuState.ResolutionIndex == test.MenuState.ResolutionIndex, "Display preferences failed to reload.");
        display.Value = new DisplayPreferences(false, new Vector2I(-1, 999999));
        restored.SettingsController.LoadDisplay();
        Check(restored.MenuState.ResolutionIndex == 2 && restoredHost.Display?.Resolution == new Vector2I(1280,800), "Invalid saved resolution lacked safe fallback.");
        display.FailSave = true;
        restored.SettingsController.ToggleFullscreen();
        Check(restored.MenuState.DisplaySaveFailed, "Display save failure was hidden.");
        test.GameInput.HandleKey(Key.Escape);
        Check(test.RunState.Screen == "home", "Main settings Escape failed.");
        test.Start(347);
        int turn = test.RunState.Turn, hp = test.PlayerState.Health, energy = test.PlayerState.Energy, fuel = test.InventoryState.TorchFuel;
        var position = test.PlayerState.Position;
        test.GameInput.HandleKey(Key.Escape); test.GameInput.HandleKey(Key.Key5);
        test.GameInput.HandleKey(Key.Down); test.GameInput.HandleKey(Key.A);
        Check(test.MenuState.PauseTab == 4 && test.MenuState.MusicVolume == 95, "Pause arrows changed tabs instead of volume.");
        test.GameInput.HandleKey(Key.Tab);
        Check(test.MenuState.PauseTab == 0, "Settings trapped tab navigation.");
        test.GameInput.HandleKey(Key.Key5); test.GameInput.HandleKey(Key.Enter);
        Check(test.RunState.Screen == "language" && test.MenuState.LanguageReturn == "pause", "Pause language entry failed.");
        test.GameInput.HandleKey(Key.Escape);
        Check(test.RunState.Screen == "pause" && test.MenuState.PauseTab == 4, "Language did not return to pause settings.");
        test.MenuState.SettingsIndex = 5; test.GameInput.HandleKey(Key.Enter);
        Check(test.RunState.Screen == "confirm_exit" && !test.MenuState.ExitYes, "Return to main menu bypassed confirmation.");
        test.GameInput.HandleKey(Key.Escape); test.GameInput.HandleKey(Key.Escape);
        Check(test.RunState.Screen == "game" && test.RunState.Turn == turn && test.PlayerState.Health == hp && test.PlayerState.Energy == energy && test.PlayerState.Position == position && test.InventoryState.TorchFuel == fuel, "Settings advanced the expedition.");
        string path = $"user://display-test-{Guid.NewGuid():N}.cfg";
        try
        {
            var store = new GodotDisplaySettings(path);
            Check(store.Load() == null, "Missing preferences should use defaults.");
            var value = new DisplayPreferences(true, new Vector2I(1920,1080));
            Check(store.Save(value) && new GodotDisplaySettings(path).Load() == value, "Display preferences did not survive disk reload.");
        }
        finally { DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path)); }
        GD.Print("SETTINGS AUDIT: both entry points, language, live independent volumes, held arrows, mute/clamps, nine resolutions, fullscreen/F11, persistence, invalid fallback, exit confirmation and turn-free navigation passed.");
    }
    private sealed class MemoryDisplaySettings : IDisplaySettings
    {
        internal DisplayPreferences? Value;
        internal bool FailSave;
        public DisplayPreferences? Load() => Value;
        public bool Save(DisplayPreferences preferences) { if (FailSave) return false; Value = preferences; return true; }
    }
}
