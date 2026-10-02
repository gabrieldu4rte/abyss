using Godot;
namespace Abyss.Presentation;
internal sealed class SettingsRenderer(AsciiCanvas canvas, Localization text, MenuState menu, UiComponents ui)
{
    internal void Draw(bool paused)
    {
        if (!paused) canvas.Text(32, 99, text.Translate("CONFIGURACOES", "SETTINGS"), UiTheme.Teal, 18);
        canvas.DrawAsciiImage(32, 200, AsciiArt.Camp, 490, 470, new Color("fff6df"), 2);
        string heading = menu.SettingsPage switch { "sound" => text.Translate("SOM", "SOUND"), "video" => text.Translate("VIDEO", "VIDEO"), _ => text.Translate("CONFIGURACOES DO JOGO", "GAME SETTINGS") };
        canvas.Text(570, 233, heading, UiTheme.Gold, 24);
        var resolution = SettingsController.Resolutions[menu.ResolutionIndex];
        string[] labels = menu.SettingsPage switch
        {
            "sound" => [text.Translate("MUSICA", "MUSIC"), text.Translate("EFEITOS SONOROS", "SOUND EFFECTS"), text.Translate("VOLTAR", "BACK")],
            "video" => [text.Translate("TELA CHEIA", "FULLSCREEN"), text.Translate("RESOLUCAO", "RESOLUTION"), text.Translate("VOLTAR", "BACK")],
            _ => [text.Translate("IDIOMA", "LANGUAGE"), text.Translate("SOM", "SOUND"), text.Translate("VIDEO", "VIDEO"), text.Translate(paused ? "VOLTAR AO MENU PRINCIPAL" : "VOLTAR", paused ? "RETURN TO MAIN MENU" : "BACK")]
        };
        string[] values = menu.SettingsPage switch
        {
            "sound" => [$"{menu.MusicVolume}%", $"{menu.EffectsVolume}%", ""],
            "video" => [menu.Fullscreen ? text.Translate("Ligado", "On") : text.Translate("Desligado", "Off"), $"{resolution.X} x {resolution.Y}", ""],
            _ => [menu.English ? "English" : "Portugues (BR)", "", "", ""]
        };
        for (int i = 0; i < labels.Length; i++)
        {
            var color = i == menu.SettingsIndex ? UiTheme.Teal : UiTheme.Ink;
            canvas.Text(570, 300 + i * 58, (i == menu.SettingsIndex ? "> " : "  ") + labels[i], color, 18);
            if (values[i] != "") canvas.Text(910, 300 + i * 58, menu.SettingsPage == "root" ? values[i] : "< " + values[i] + " >", color, 18);
        }
        string hint = menu.SettingsPage switch
        {
            "sound" => text.Translate("A/D ou esquerda/direita: ajustar 5%. 0% silencia.", "A/D or left/right: adjust by 5%. 0% mutes."),
            "video" => text.Translate("A/D ajusta. [ENTER] alterna tela cheia. [F11] atalho.", "A/D adjusts. [ENTER] toggles fullscreen. [F11] shortcut."),
            _ => text.Translate("[ENTER] abrir a categoria selecionada.", "[ENTER] open the selected category.")
        };
        canvas.Text(570, 610, hint, UiTheme.Dim, 16);
        canvas.Text(570, 640, text.Translate("[ESC] voltar", "[ESC] back"), UiTheme.Dim, 16);
        if (menu.DisplaySaveFailed) canvas.Text(570, 675, text.Translate("Nao foi possivel salvar as preferencias de video.", "Could not save display preferences."), UiTheme.Red, 15);
        if (!paused) ui.Footer("[CIMA/BAIXO / W S] selecionar   [ENTER] abrir   [ESC] voltar", "[UP/DOWN / W S] select   [ENTER] open   [ESC] back");
    }
}
