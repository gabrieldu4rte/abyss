namespace Abyss.Infrastructure;
internal sealed class LanguagePreferences
{
    private readonly MenuState menuState;
    private readonly ILanguageSettings settings;
    internal LanguagePreferences(MenuState menuState, ILanguageSettings settings)
    {
        this.menuState = menuState;
        this.settings = settings;
    }

    internal const string SettingsPath = "user://settings.cfg";
    internal void LoadLanguage(string path = SettingsPath)
    {
        if (settings.LoadEnglish(path)is bool english)
            menuState.English = english;
    }

    internal void SaveLanguage(string path = SettingsPath)
    {
        if (menuState.IsTesting && path == SettingsPath)
            return;
        menuState.LanguageNotice = settings.SaveEnglish(path, menuState.English) ? "" : "save_error";
    }
}
