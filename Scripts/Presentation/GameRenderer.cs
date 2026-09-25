namespace Abyss.Presentation;
internal sealed class GameRenderer
{
    private readonly AsciiCanvas asciiCanvas;
    private readonly HudRenderer hudRenderer;
    private readonly Localization localization;
    private readonly MenuRenderer menuRenderer;
    private readonly MerchantRenderer merchantRenderer;
    private readonly PauseRenderer pauseRenderer;
    private readonly RunState runState;
    internal GameRenderer(AsciiCanvas asciiCanvas, HudRenderer hudRenderer, Localization localization, MenuRenderer menuRenderer, MerchantRenderer merchantRenderer, PauseRenderer pauseRenderer, RunState runState)
    {
        this.asciiCanvas = asciiCanvas;
        this.hudRenderer = hudRenderer;
        this.localization = localization;
        this.menuRenderer = menuRenderer;
        this.merchantRenderer = merchantRenderer;
        this.pauseRenderer = pauseRenderer;
        this.runState = runState;
    }

    public void Draw()
    {
        if (asciiCanvas.Font == null)
            return;
        asciiCanvas.Text(32, 34, localization.Translate("A B I S M O", "A B Y S S"), UiTheme.Gold, 22);
        asciiCanvas.Rule(54);
        switch (runState.Screen)
        {
            case "home":
                menuRenderer.DrawHome();
                return;
            case "language":
                menuRenderer.DrawLanguage();
                return;
            case "classes":
                menuRenderer.DrawClasses();
                return;
            case "pause":
                pauseRenderer.DrawPause();
                return;
            case "shop":
                merchantRenderer.DrawShop();
                return;
            case "confirm_exit":
                menuRenderer.DrawExitConfirm();
                return;
            case "dead":
                menuRenderer.DrawEnd();
                return;
        }

        hudRenderer.DrawGame();
    }
}
