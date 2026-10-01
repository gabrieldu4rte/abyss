using Godot;
namespace Abyss.Presentation;
internal sealed class SettingsRenderer(AsciiCanvas canvas, Localization text, MenuState menu, UiComponents ui)
{
    internal void Draw(bool paused)
    {
        if (!paused) canvas.Text(32, 99, text.Translate("CONFIGURACOES", "SETTINGS"), UiTheme.Teal, 18);
        canvas.DrawAsciiImage(32, 200, AsciiArt.Camp, 490, 470, new Color("fff6df"), 2);
        canvas.Text(570, 233, text.Translate("CONFIGURACOES DO JOGO", "GAME SETTINGS"), UiTheme.Gold, 24);
        string[] labels = [text.Translate("IDIOMA", "LANGUAGE"), text.Translate("MUSICA", "MUSIC"), text.Translate("EFEITOS SONOROS", "SOUND EFFECTS"), text.Translate("TELA CHEIA", "FULLSCREEN"), text.Translate("RESOLUCAO", "RESOLUTION"), text.Translate(paused ? "VOLTAR AO MENU PRINCIPAL" : "VOLTAR", paused ? "RETURN TO MAIN MENU" : "BACK")];
        var resolution = SettingsController.Resolutions[menu.ResolutionIndex];
        string[] values = [menu.English ? "English" : "Portugues (BR)", $"{menu.MusicVolume}%", $"{menu.EffectsVolume}%", menu.Fullscreen ? text.Translate("Ligado", "On") : text.Translate("Desligado", "Off"), $"{resolution.X} x {resolution.Y}", ""];
        for (int i = 0; i < labels.Length; i++)
        {
            var color = i == menu.SettingsIndex ? UiTheme.Teal : UiTheme.Ink;
            canvas.Text(570, 300 + i * 54, (i == menu.SettingsIndex ? "> " : "  ") + labels[i], color, 18);
            if (values[i] != "") canvas.Text(910, 300 + i * 54, "< " + values[i] + " >", color, 18);
        }
        string hint = menu.SettingsIndex switch
        {
            0 => text.Translate("[ENTER] selecionar idioma; A/D alterna.", "[ENTER] choose language; A/D switches."),
            1 or 2 => text.Translate("A/D ou esquerda/direita: ajustar 5%. 0% silencia.", "A/D or left/right: adjust by 5%. 0% mutes."),
            3 => text.Translate("[ENTER] ou A/D alterna. [F11] tambem funciona.", "[ENTER] or A/D toggles. [F11] also works."),
            4 => text.Translate("A/D altera a resolucao, mantendo as proporcoes.", "A/D changes resolution, preserving proportions."),
            _ => paused ? text.Translate("[ENTER] voltar ao menu. [ESC] retomar aventura.", "[ENTER] return to menu. [ESC] resume adventure.") : text.Translate("[ENTER] ou [ESC] voltar ao menu principal.", "[ENTER] or [ESC] return to the main menu.")
        };
        canvas.Text(570, 640, hint, UiTheme.Dim, 16);
        if (menu.DisplaySaveFailed) canvas.Text(570, 675, text.Translate("Nao foi possivel salvar as preferencias de video.", "Could not save display preferences."), UiTheme.Red, 15);
        if (!paused) ui.Footer("[CIMA/BAIXO / W S] selecionar   [ESQ/DIR / A D] ajustar   [ENTER] escolher   [ESC] voltar", "[UP/DOWN / W S] select   [LEFT/RIGHT / A D] adjust   [ENTER] choose   [ESC] back");
    }
}
