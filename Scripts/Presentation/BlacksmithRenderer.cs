using System;
namespace Abyss.Presentation;
internal sealed class BlacksmithRenderer(AsciiCanvas canvas, Localization text, InventoryState inventory, PlayerState player, MenuState menu, UiComponents ui)
{
    internal void Draw()
    {
        canvas.Text(32, 96, text.Translate("O FERREIRO PERDIDO", "THE LOST BLACKSMITH"), UiTheme.Gold, 24);
        canvas.Portrait(32, 158, AsciiArt.Blacksmith, text.Translate("FERREIRO PERDIDO", "LOST BLACKSMITH"), UiTheme.Gold);
        canvas.WrappedText(32, 490, text.Translate("Ainda ha uma centelha em seu equipamento. Posso desperta-la.", "There is still a spark in your equipment. I can awaken it."), UiTheme.Ink, 16, 28, 24);
        canvas.Text(32, 640, text.Translate($"OURO {player.Gold}", $"GOLD {player.Gold}"), UiTheme.Gold, 20);
        if (menu.PendingUpgrade is Gear gear)
        {
            var next = gear with { Quality = gear.Quality + 1 };
            canvas.Text(350, 180, text.Translate("CONFIRMAR MELHORIA?", "CONFIRM UPGRADE?"), UiTheme.Gold, 23);
            canvas.Text(350, 240, text.OfferName(new Offer { Gear = gear }), UiTheme.RarityColor(gear.Quality), 18);
            canvas.Text(350, 280, "> " + text.OfferName(new Offer { Gear = next }), UiTheme.RarityColor(next.Quality), 18);
            canvas.Text(350, 330, text.Translate($"Custo: {BlacksmithService.Price(gear)} ouro", $"Cost: {BlacksmithService.Price(gear)} gold"), UiTheme.Ink, 20);
            canvas.WrappedText(350, 375, text.GearStats(next), UiTheme.Ink, 16, 85, 24);
            canvas.Text(350, 440, text.Translate($"Requer nivel {next.RequiredLevel} para equipar.", $"Requires level {next.RequiredLevel} to equip."), UiTheme.Gold, 17);
            if (player.Level < next.RequiredLevel) canvas.Text(350, 478, text.Translate("O item ficara na mochila, sem ser equipado.", "The item will remain unequipped in your backpack."), UiTheme.Dim, 17);
            canvas.Text(350, 550, (menu.ConfirmYes ? "  " : "> ") + text.Translate("NAO, CANCELAR", "NO, CANCEL"), UiTheme.Teal, 20);
            canvas.Text(350, 595, (menu.ConfirmYes ? "> " : "  ") + text.Translate("SIM, MELHORAR", "YES, UPGRADE"), UiTheme.Teal, 20);
        }
        else
        {
            canvas.Text(350, 150, text.Translate("ESCOLHA UM EQUIPAMENTO", "CHOOSE EQUIPMENT"), UiTheme.Teal, 20);
            int count = inventory.Backpack.Count;
            menu.BlacksmithIndex = Math.Clamp(menu.BlacksmithIndex, 0, Math.Max(0, count - 1));
            int start = menu.BlacksmithIndex / 9 * 9;
            for (int i = start; i < Math.Min(start + 9, count); i++)
            {
                var item = inventory.Backpack[i];
                string suffix = BlacksmithService.CanUpgrade(item) ? $" | {BlacksmithService.Price(item)} " + text.Translate("ouro", "gold") : text.Translate(" | sem melhoria", " | cannot upgrade");
                canvas.Text(350, 205 + (i - start) * 38, (i == menu.BlacksmithIndex ? "> " : "  ") + text.OfferName(new Offer { Gear = item }) + suffix, UiTheme.RarityColor(item.Quality), 16);
            }
            if (count == 0) canvas.Text(350, 220, text.Translate("Nenhum equipamento na mochila.", "No equipment in your backpack."), UiTheme.Dim, 18);
            canvas.WrappedText(350, 580, text.Translate("Itens unicos nao podem ser reforjados. Lendario e a maior raridade.", "Unique items cannot be reforged. Legendary is the highest rarity."), UiTheme.Dim, 17, 85, 25);
            canvas.Text(350, 657, text.Translate(menu.ShopNotice.Pt, menu.ShopNotice.En), UiTheme.Gold, 17);
        }
        ui.Footer("[SETAS] escolher   [ENTER] confirmar   [ESC] voltar", "[ARROWS] choose   [ENTER] confirm   [ESC] back");
    }
}
