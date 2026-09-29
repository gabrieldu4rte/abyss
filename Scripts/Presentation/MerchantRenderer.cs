using System;
using System.Linq;

namespace Abyss.Presentation;
internal sealed class MerchantRenderer
{
    private readonly AsciiCanvas asciiCanvas;
    private readonly InventoryState inventoryState;
    private readonly Localization localization;
    private readonly MenuState menuState;
    private readonly MerchantService merchantService;
    private readonly PlayerState playerState;
    private readonly UiComponents uiComponents;
    internal MerchantRenderer(AsciiCanvas asciiCanvas, InventoryState inventoryState, Localization localization, MenuState menuState, MerchantService merchantService, PlayerState playerState, UiComponents uiComponents)
    {
        this.asciiCanvas = asciiCanvas;
        this.inventoryState = inventoryState;
        this.localization = localization;
        this.menuState = menuState;
        this.merchantService = merchantService;
        this.playerState = playerState;
        this.uiComponents = uiComponents;
    }

    internal void DrawShop()
    {
        asciiCanvas.Text(32, 96, localization.Translate("O MERCADOR DO ABISMO", "THE ABYSS MERCHANT"), UiTheme.Gold, 24);
        asciiCanvas.Portrait(32, 158, AsciiArt.Merchant, localization.Translate("MERCADOR", "MERCHANT"), UiTheme.Teal);
        asciiCanvas.Text(32, 490, localization.Translate($"OURO {playerState.Gold}", $"GOLD {playerState.Gold}"), UiTheme.Gold, 21);
        string speech = localization.MerchantSpeech();
        int row = 0;
        while (speech.Length > 0)
        {
            int cut = Math.Min(28, speech.Length);
            if (cut < speech.Length)
            {
                int space = speech.LastIndexOf(' ', cut - 1);
                if (space > 0)
                    cut = space;
            }

            asciiCanvas.Text(32, 535 + row++ * 24, speech[..cut], UiTheme.Ink, 14);
            speech = speech[cut..].TrimStart();
        }

        asciiCanvas.Text(340, 145, (menuState.ShopSelling ? "  " : "> ") + localization.Translate("COMPRAR", "BUY"), menuState.ShopSelling ? UiTheme.Dim : UiTheme.Teal, 21);
        asciiCanvas.Text(640, 145, (menuState.ShopSelling ? "> " : "  ") + localization.Translate("VENDER", "SELL"), menuState.ShopSelling ? UiTheme.Teal : UiTheme.Dim, 21);
        if (menuState.PendingTrade is Offer pending)
        {
            asciiCanvas.Frame(335, 183, 96, 23, UiTheme.Dim, 15, 20);
            asciiCanvas.Text(358, 227, menuState.ShopSelling ? localization.Translate("CONFIRMAR VENDA?", "CONFIRM SALE?") : localization.Translate("CONFIRMAR COMPRA?", "CONFIRM PURCHASE?"), UiTheme.Gold, 23);
            asciiCanvas.Text(358, 275, localization.OfferName(pending), pending.Gear is Gear g ? UiTheme.RarityColor(g.Quality) : UiTheme.Ink, 20);
            asciiCanvas.Text(358, 319, localization.Translate($"Quantidade: 1  |  Valor: {merchantService.TradePrice(pending)} ouro", $"Quantity: 1  |  Price: {merchantService.TradePrice(pending)} gold"), UiTheme.Ink, 19);
            if (menuState.ShopSelling && pending.Gear is Gear worn && inventoryState.Equipped.Any(g => ReferenceEquals(g, worn)))
                asciiCanvas.Text(358, 365, localization.Translate("Este item esta equipado e sera removido.", "This item is equipped and will be removed."), UiTheme.Red, 17);
            asciiCanvas.Text(358, 436, (menuState.ConfirmYes ? "  " : "> ") + localization.Translate("NAO, CANCELAR", "NO, CANCEL"), menuState.ConfirmYes ? UiTheme.Dim : UiTheme.Teal, 21);
            asciiCanvas.Text(358, 492, (menuState.ConfirmYes ? "> " : "  ") + localization.Translate("SIM, CONFIRMAR", "YES, CONFIRM"), menuState.ConfirmYes ? UiTheme.Teal : UiTheme.Dim, 21);
            uiComponents.Footer("[SETAS] escolher   [ENTER] confirmar   [ESC] cancelar", "[ARROWS] choose   [ENTER] confirm   [ESC] cancel");
            return;
        }

        var offers = merchantService.ShopOffers();
        menuState.ShopIndex = Math.Clamp(menuState.ShopIndex, 0, Math.Max(0, offers.Count - 1));
        int page = menuState.ShopIndex / 7;
        if (offers.Count == 0)
            asciiCanvas.Text(350, 215, localization.Translate("Nenhum item disponivel.", "No items available."), UiTheme.Dim, 19);
        for (int i = page * 7; i < Math.Min(offers.Count, page * 7 + 7); i++)
        {
            var o = offers[i];
            asciiCanvas.Text(345, 204 + (i - page * 7) * 35, (i == menuState.ShopIndex ? "> " : "  ") + localization.OfferName(o) + $" x{o.Quantity} | {merchantService.TradePrice(o)} " + localization.Translate("ouro", "gold"), o.Gear is Gear g ? UiTheme.RarityColor(g.Quality) : UiTheme.Ink, 17);
        }

        asciiCanvas.Text(345, 462, localization.Translate($"PAGINA {page + 1}/{Math.Max(1, (offers.Count + 6) / 7)}", $"PAGE {page + 1}/{Math.Max(1, (offers.Count + 6) / 7)}"), UiTheme.Dim, 14);
        if (offers.Count > 0)
        {
            var o = offers[menuState.ShopIndex];
            if (o.Gear is Gear g)
            {
                string classes = string.Join(" / ", Enumerable.Range(0, 4).Where(g.Allows).Select(localization.ClassName));
                asciiCanvas.Text(345, 501, localization.Translate($"Requer nivel {g.RequiredLevel}: ", $"Requires level {g.RequiredLevel}: ") + classes, playerState.Level >= g.RequiredLevel && g.Allows(playerState.ClassIndex) ? UiTheme.Teal : UiTheme.Red, 15);
                string stats = localization.GearStats(g);
                asciiCanvas.Text(345, 535, stats, UiTheme.Ink, 16);
                asciiCanvas.Text(345, 568, localization.GearEffect(g), UiTheme.Ink, 16);
                if (!g.Allows(playerState.ClassIndex) || playerState.Level < g.RequiredLevel)
                    asciiCanvas.Text(345, 600, localization.Translate("Pode comprar e guardar; ainda nao pode equipar.", "You may buy and keep it, but cannot equip it yet."), UiTheme.Dim, 15);
            }
            else
                asciiCanvas.Text(345, 510, o.Potion == 2 ? localization.Translate("Ilumina por 100 turnos. Slot proprio; pode ser arremessada.", "Lights 100 turns. Dedicated slot; can be thrown.") : o.Potion == 0 ? localization.Translate("Cura 2d10 PV (2-20).", "Heals 2d10 HP (2-20).") : localization.Translate("Restaura 2d6 energia (2-12).", "Restores 2d6 energy (2-12)."), UiTheme.Ink, 18);
        }

        asciiCanvas.Text(345, 668, localization.Translate(menuState.ShopNotice.Pt, menuState.ShopNotice.En), UiTheme.Gold, 17);
        uiComponents.Footer("[A D / TAB] comprar/vender   [CIMA/BAIXO] item   [ENTER] negociar   [ESC] sair", "[A D / TAB] buy/sell   [UP/DOWN] item   [ENTER] trade   [ESC] leave");
    }
}
