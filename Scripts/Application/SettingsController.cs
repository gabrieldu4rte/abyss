using Godot;
using System;
namespace Abyss.Application;
internal sealed class SettingsController(MenuState menu, RunState run, IGameHost host, LanguagePreferences language, IDisplaySettings? displayStore, IGameAudio? audio)
{
    internal static readonly Vector2I[] Resolutions =
    [new(1024,640), new(1280,720), new(1280,800), new(1366,768), new(1600,900), new(1920,1080), new(1920,1200), new(2560,1440), new(3840,2160)];
    internal DisplayPreferences Display => new(menu.Fullscreen, Resolutions[menu.ResolutionIndex]);
    internal void LoadDisplay()
    {
        var saved = displayStore?.Load();
        if (saved != null)
        {
            menu.Fullscreen = saved.Fullscreen;
            int index = Array.IndexOf(Resolutions, saved.Resolution);
            menu.ResolutionIndex = index < 0 ? 2 : index;
        }
        host.ApplyDisplay(Display);
    }
    private void ApplyDisplay()
    {
        host.ApplyDisplay(Display);
        menu.DisplaySaveFailed = !menu.IsTesting && displayStore != null && !displayStore.Save(Display);
    }
    internal void ToggleFullscreen()
    {
        menu.Fullscreen = !menu.Fullscreen;
        ApplyDisplay();
    }
    internal void Open()
    {
        menu.SettingsIndex = 0;
        run.Screen = "settings";
    }
    internal void Handle(Key key, bool paused)
    {
        if (key == Key.Escape)
        {
            run.Screen = paused ? "game" : "home";
            if (!paused) menu.MenuIndex = 1;
            return;
        }
        if (UiTheme.Previous(key)) menu.SettingsIndex = (menu.SettingsIndex + 5) % 6;
        if (UiTheme.Next(key)) menu.SettingsIndex = (menu.SettingsIndex + 1) % 6;
        int direction = key is Key.Left or Key.A ? -1 : key is Key.Right or Key.D ? 1 : 0;
        if (direction != 0)
        {
            switch (menu.SettingsIndex)
            {
                case 0: menu.English = !menu.English; language.SaveLanguage(); break;
                case 1: menu.MusicVolume = Math.Clamp(menu.MusicVolume + direction * 5, 0, 100); audio?.SetVolumes(menu.MusicVolume, menu.EffectsVolume); break;
                case 2: menu.EffectsVolume = Math.Clamp(menu.EffectsVolume + direction * 5, 0, 100); audio?.SetVolumes(menu.MusicVolume, menu.EffectsVolume); break;
                case 3: ToggleFullscreen(); break;
                case 4:
                    menu.ResolutionIndex = Math.Clamp(menu.ResolutionIndex + direction, 0, Resolutions.Length - 1);
                    ApplyDisplay(); break;
            }
        }
        if (!UiTheme.Confirm(key)) return;
        if (menu.SettingsIndex == 0)
        {
            menu.LanguageReturn = paused ? "pause" : "settings";
            menu.LanguageIndex = menu.English ? 1 : 0;
            run.Screen = "language";
        }
        else if (menu.SettingsIndex == 3) ToggleFullscreen();
        else if (menu.SettingsIndex == 5)
        {
            if (paused) { menu.ExitYes = false; run.Screen = "confirm_exit"; }
            else { run.Screen = "home"; menu.MenuIndex = 1; }
        }
    }
}
