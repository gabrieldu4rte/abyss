using System;
using System.Linq;

namespace Abyss.Presentation;
internal sealed class InventoryRenderer
{
    private readonly AsciiCanvas asciiCanvas;
    private readonly InventoryState inventoryState;
    private readonly Localization localization;
    private readonly MenuState menuState;
    private readonly PlayerState playerState;
    internal InventoryRenderer(AsciiCanvas asciiCanvas, InventoryState inventoryState, Localization localization, MenuState menuState, PlayerState playerState)
    {
        this.asciiCanvas = asciiCanvas;
        this.inventoryState = inventoryState;
        this.localization = localization;
        this.menuState = menuState;
        this.playerState = playerState;
    }

    internal void DrawInventory()
    {
        asciiCanvas.Text(32, 202, localization.Translate("EQUIPADO", "EQUIPPED"), UiTheme.Gold, 16);
        for (int i = 0; i < 3; i++)
        {
            var g = inventoryState.Equipped[i];
            asciiCanvas.Text(32, 234 + i * 31, (menuState.InventoryIndex == i ? "> " : "  ") + localization.SlotName(i) + ": " + (g == null ? localization.Translate("Vazio", "Empty") : localization.GearLabel(g)), g == null ? UiTheme.Dim : UiTheme.RarityColor(g.Quality), 15);
        }

        asciiCanvas.Text(32, 334, localization.Translate("CONSUMIVEIS", "CONSUMABLES"), UiTheme.Gold, 16);
        asciiCanvas.Text(32, 363, (menuState.InventoryIndex == 3 ? "> " : "  ") + localization.Translate($"Pocao de vida x{inventoryState.Potions}", $"Health potion x{inventoryState.Potions}"), menuState.InventoryIndex == 3 ? UiTheme.Teal : UiTheme.Ink, 16);
        asciiCanvas.Text(32, 391, (menuState.InventoryIndex == 4 ? "> " : "  ") + localization.Translate($"Pocao de energia x{inventoryState.EnergyPotions}", $"Energy potion x{inventoryState.EnergyPotions}"), menuState.InventoryIndex == 4 ? UiTheme.Teal : UiTheme.Ink, 16);
        asciiCanvas.Text(32, 419, (menuState.InventoryIndex == 5 ? "> " : "  ") + localization.Translate($"Tochas x{inventoryState.TorchCount} | Atual: {inventoryState.TorchFuel}", $"Torches x{inventoryState.TorchCount} | Current: {inventoryState.TorchFuel}"), menuState.InventoryIndex == 5 ? UiTheme.Teal : UiTheme.Ink, 16);
        int page = Math.Max(0, menuState.InventoryIndex - 6) / 5;
        asciiCanvas.Text(32, 457, localization.Translate($"MOCHILA / PAGINA {page + 1}", $"BACKPACK / PAGE {page + 1}"), UiTheme.Gold, 16);
        for (int i = page * 5; i < Math.Min(inventoryState.Backpack.Count, page * 5 + 5); i++)
        {
            var g = inventoryState.Backpack[i];
            bool worn = inventoryState.Equipped.Any(e => ReferenceEquals(e, g));
            asciiCanvas.Text(32, 488 + (i - page * 5) * 29, (menuState.InventoryIndex == i + 6 ? "> " : "  ") + (worn ? "[*] " : "[ ] ") + localization.GearLabel(g), UiTheme.RarityColor(g.Quality), 16);
        }

        asciiCanvas.Frame(570, 198, 72, 26, UiTheme.Dim, 15, 18);
        Gear? detail = menuState.InventoryIndex < 3 ? inventoryState.Equipped[menuState.InventoryIndex] : menuState.InventoryIndex >= 6 ? inventoryState.Backpack[menuState.InventoryIndex - 6] : null;
        if (detail is Gear item)
        {
            asciiCanvas.Text(590, 232, localization.GearLabel(item), UiTheme.RarityColor(item.Quality), 21);
            asciiCanvas.Text(590, 270, $"{localization.SlotName((int)item.Slot)} / {localization.Translate("VALOR", "VALUE")} {item.Value} {localization.Translate("OURO", "GOLD")}", UiTheme.Ink, 16);
            asciiCanvas.Text(590, 308, localization.Translate($"Requer nivel {item.RequiredLevel}", $"Requires level {item.RequiredLevel}"), playerState.Level >= item.RequiredLevel ? UiTheme.Teal : UiTheme.Red, 17);
            string classes = string.Join(" / ", Enumerable.Range(0, 4).Where(item.Allows).Select(localization.ClassName));
            asciiCanvas.Text(590, 340, classes, item.Allows(playerState.ClassIndex) ? UiTheme.Teal : UiTheme.Red, 14);
            string stats = localization.GearStats(item);
            asciiCanvas.Text(590, 388, stats, UiTheme.Ink, 16);
            asciiCanvas.Text(590, 438, localization.Translate("EFEITO ESPECIAL", "SPECIAL EFFECT"), UiTheme.Gold, 16);
            asciiCanvas.Text(590, 471, localization.GearEffect(item), UiTheme.Ink, 15);
            asciiCanvas.Text(590, 526, localization.Translate("[ENTER] equipar / remover", "[ENTER] equip / remove"), UiTheme.Teal, 17);
        }
        else if (menuState.InventoryIndex == 5)
        {
            asciiCanvas.Text(590, 240, localization.Translate("TOCHA", "TORCH"), UiTheme.Gold, 22);
            asciiCanvas.Lines(590, 290, localization.Translate(
                "Ilumina por 100 turnos. Valor: 8 ouro.\nSem luz, sua visao fica reduzida.\nA proxima reserva acende automaticamente.\n\n[ENTER] acender / guardar\n[T] arremessar e escolher direcao\n\nO fogo inflama oleo e fere qualquer um.\nAgua apaga uma tocha arremessada.",
                "Lights 100 turns. Value: 8 gold.\nWithout light, your sight is reduced.\nThe next spare lights automatically.\n\n[ENTER] light / stow\n[T] throw and choose direction\n\nFire ignites oil and hurts anyone.\nWater puts out a thrown torch."), UiTheme.Ink, 17, 29);
        }
        else if (menuState.InventoryIndex is 3 or 4)
        {
            bool life = menuState.InventoryIndex == 3;
            asciiCanvas.Text(590, 240, life ? localization.Translate("POCAO DE VIDA", "HEALTH POTION") : localization.Translate("POCAO DE ENERGIA", "ENERGY POTION"), UiTheme.Teal, 22);
            asciiCanvas.Lines(590, 300, life ? localization.Translate("Cura 2d10 PV (2-20).\nValor: 12 ouro.\nQualquer classe; nivel 1.", "Heals 2d10 HP (2-20).\nValue: 12 gold.\nAny class; level 1.") : localization.Translate("Restaura 2d6 energia (2-12).\nValor: 15 ouro.\nQualquer classe; nivel 1.", "Restores 2d6 energy (2-12).\nValue: 15 gold.\nAny class; level 1."), UiTheme.Ink, 18, 34);
            asciiCanvas.Text(590, 450, localization.Translate("[ENTER] beber", "[ENTER] drink"), UiTheme.Teal, 18);
        }
        else
            asciiCanvas.Text(590, 240, localization.Translate("Selecione um item na mochila.", "Select an item in the backpack."), UiTheme.Ink, 18);
        asciiCanvas.Text(590, 574, localization.Translate("Usar ou equipar gasta um turno", "Using or equipping uses one turn"), UiTheme.Dim, 16);
        asciiCanvas.Text(590, 601, localization.Translate("e retoma a partida.", "and resumes the game."), UiTheme.Dim, 16);
        asciiCanvas.Text(32, 692, localization.Translate(menuState.InventoryNotice.Pt, menuState.InventoryNotice.En), UiTheme.Red, 16);
    }
}
