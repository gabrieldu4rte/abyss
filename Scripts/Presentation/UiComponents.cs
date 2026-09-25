namespace Abyss.Presentation;
internal sealed class UiComponents
{
    private readonly AsciiCanvas asciiCanvas;
    private readonly Localization localization;
    private readonly MenuState menuState;
    internal UiComponents(AsciiCanvas asciiCanvas, Localization localization, MenuState menuState)
    {
        this.asciiCanvas = asciiCanvas;
        this.localization = localization;
        this.menuState = menuState;
    }

    internal void MenuItem(float x, float y, int index, string label)
    {
        asciiCanvas.Text(x, y, (menuState.MenuIndex == index ? "> [ " : "  [ ") + label + " ]", menuState.MenuIndex == index ? UiTheme.Teal : UiTheme.Dim, 23);
    }

    internal void Footer(string pt, string en)
    {
        asciiCanvas.Rule(709);
        asciiCanvas.Text(32, 748, localization.Translate(pt, en), UiTheme.Teal, 17);
        if (menuState.LanguageNotice != "")
            asciiCanvas.Text(32, 781, localization.Translate("Nao foi possivel salvar o idioma.", "Could not save the language preference."), UiTheme.Red, 14);
    }
}
