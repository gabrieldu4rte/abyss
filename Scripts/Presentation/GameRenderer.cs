namespace Abyss.Presentation;
internal sealed class GameRenderer
{
    private readonly SettingsRenderer settingsRenderer;
    private readonly BlacksmithRenderer blacksmith;
    private readonly AsciiCanvas asciiCanvas;
    private readonly MenuState menuState;
    private readonly ScreenTransitions transitions;
    private readonly OpeningStory openingStory;
    private readonly HudRenderer hudRenderer;
    private readonly Localization localization;
    private readonly MenuRenderer menuRenderer;
    private readonly MerchantRenderer merchantRenderer;
    private readonly PauseRenderer pauseRenderer;
    private readonly RunState runState;
    internal GameRenderer(AsciiCanvas asciiCanvas, HudRenderer hudRenderer, Localization localization, MenuRenderer menuRenderer, MerchantRenderer merchantRenderer, PauseRenderer pauseRenderer, RunState runState, MenuState menuState, ScreenTransitions transitions, OpeningStory openingStory, BlacksmithRenderer blacksmith, SettingsRenderer settingsRenderer)
    {
        this.settingsRenderer = settingsRenderer;
        this.blacksmith = blacksmith;
        this.asciiCanvas = asciiCanvas;
        this.menuState = menuState;
        this.transitions = transitions;
        this.openingStory = openingStory;
        this.hudRenderer = hudRenderer;
        this.localization = localization;
        this.menuRenderer = menuRenderer;
        this.merchantRenderer = merchantRenderer;
        this.pauseRenderer = pauseRenderer;
        this.runState = runState;
    }

    public void Draw()
    {
        string route = runState.Screen + (runState.Screen == "pause" ? $":{menuState.PauseTab}" : runState.Screen == "shop" ? $":{menuState.ShopSelling}:{menuState.PendingTrade != null}" : "");
        if (runState.Screen == "blacksmith") route += $":{menuState.PendingUpgrade != null}";
        transitions.BeginFrame(route);
        DrawContent();
        transitions.EndFrame();
    }

    private void DrawContent()
    {
        if (asciiCanvas.Font == null)
            return;
        if (runState.Screen == "intro") { openingStory.Draw(asciiCanvas); return; }
        asciiCanvas.Text(32, 34, localization.Translate("A B I S M O", "A B Y S S"), UiTheme.Gold, 22);
        asciiCanvas.Rule(54);
        switch (runState.Screen)
        {
            case "home":
                menuRenderer.DrawHome();
                return;
            case "settings":
                settingsRenderer.Draw(false);
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
            case "blacksmith":
                blacksmith.Draw();
                return;
            case "shop":
                merchantRenderer.DrawShop();
                return;
            case "confirm_quit":
                menuRenderer.DrawQuitConfirm();
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
