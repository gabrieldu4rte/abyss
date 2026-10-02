using Godot;
using System;
namespace Abyss.Application;
internal sealed class SettingsController(MenuState menu, RunState run, IGameHost host, IDisplaySettings? displayStore, IGameAudio? audio)
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
        menu.SettingsPage = "root";
        menu.SettingsIndex = 0;
        run.Screen = "settings";
    }
    internal void Handle(Key key, bool paused)
    {
        if (key == Key.Escape)
        {
            if (menu.SettingsPage != "root") { BackToCategories(); return; }
            run.Screen = paused ? "game" : "home";
            if (!paused) menu.MenuIndex = 1;
            return;
        }
        int count = menu.SettingsPage == "root" ? 4 : 3;
        if (UiTheme.Previous(key)) menu.SettingsIndex = (menu.SettingsIndex + count - 1) % count;
        if (UiTheme.Next(key)) menu.SettingsIndex = (menu.SettingsIndex + 1) % count;
        if (menu.SettingsPage == "root")
        {
            if (!UiTheme.Confirm(key)) return;
            switch (menu.SettingsIndex)
            {
                case 0:
                    menu.LanguageReturn = paused ? "pause" : "settings";
                    menu.LanguageIndex = menu.English ? 1 : 0;
                    run.Screen = "language";
                    break;
                case 1: menu.SettingsPage = "sound"; menu.SettingsIndex = 0; break;
                case 2: menu.SettingsPage = "video"; menu.SettingsIndex = 0; break;
                case 3:
                    if (paused) { menu.ExitYes = false; run.Screen = "confirm_exit"; }
                    else { run.Screen = "home"; menu.MenuIndex = 1; }
                    break;
            }
            return;
        }
        if (UiTheme.Confirm(key) && menu.SettingsIndex == 2) { BackToCategories(); return; }
        int direction = key is Key.Left or Key.A ? -1 : key is Key.Right or Key.D ? 1 : 0;
        if (menu.SettingsPage == "sound" && direction != 0 && menu.SettingsIndex < 2)
        {
            if (menu.SettingsIndex == 0) menu.MusicVolume = Math.Clamp(menu.MusicVolume + direction * 5, 0, 100);
            else menu.EffectsVolume = Math.Clamp(menu.EffectsVolume + direction * 5, 0, 100);
            audio?.SetVolumes(menu.MusicVolume, menu.EffectsVolume);
        }
        if (menu.SettingsPage == "video")
        {
            if (menu.SettingsIndex == 0 && (direction != 0 || UiTheme.Confirm(key))) ToggleFullscreen();
            if (menu.SettingsIndex == 1 && direction != 0)
            {
                menu.ResolutionIndex = Math.Clamp(menu.ResolutionIndex + direction, 0, Resolutions.Length - 1);
                ApplyDisplay();
            }
        }
    }
    private void BackToCategories()
    {
        menu.SettingsIndex = menu.SettingsPage == "sound" ? 1 : 2;
        menu.SettingsPage = "root";
    }
}
