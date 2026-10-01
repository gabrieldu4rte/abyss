using System;
using Godot;
namespace Abyss.Application;
internal sealed class BlacksmithService(InventoryState inventory, PlayerState player, MenuState menu, RunState run, ExpeditionJournal journal)
{
    internal static bool CanUpgrade(Gear gear) => gear.Special == ItemId.None && gear.Quality < Rarity.Legendary;
    internal static int Price(Gear gear) => gear.Quality switch { Rarity.Common => 60, Rarity.Rare => 180, Rarity.Epic => 450, _ => 0 };
    internal void CompleteUpgrade()
    {
        var gear = menu.PendingUpgrade;
        menu.PendingUpgrade = null;
        if (gear == null || !CanUpgrade(gear)) return;
        int index = inventory.Backpack.FindIndex(g => ReferenceEquals(g, gear));
        if (index < 0) return;
        int price = Price(gear);
        if (player.Gold < price) { menu.ShopNotice = ("Ouro insuficiente.", "Not enough gold."); return; }
        var upgraded = gear with { Quality = gear.Quality + 1 };
        player.Gold -= price;
        inventory.Backpack[index] = upgraded;
        for (int slot = 0; slot < inventory.Equipped.Length; slot++)
            if (ReferenceEquals(inventory.Equipped[slot], gear))
                inventory.Equipped[slot] = player.Level >= upgraded.RequiredLevel && upgraded.Allows(player.ClassIndex) ? upgraded : null;
        run.IsAiming = false;
        menu.ShopNotice = ("A forja revelou uma nova raridade.", "The forge revealed a new rarity.");
        journal.Say(menu.ShopNotice.Pt, menu.ShopNotice.En);
    }
    internal void Handle(Key key)
    {
        if (menu.PendingUpgrade != null)
        {
            if (key == Key.Escape) { menu.PendingUpgrade = null; return; }
            if (UiTheme.Previous(key) || UiTheme.Next(key) || key == Key.Left || key == Key.Right) menu.ConfirmYes = !menu.ConfirmYes;
            if (UiTheme.Confirm(key)) { if (menu.ConfirmYes) CompleteUpgrade(); else menu.PendingUpgrade = null; }
            return;
        }
        if (key == Key.Escape) { run.Screen = "game"; return; }
        int count = inventory.Backpack.Count;
        if (count == 0) return;
        menu.BlacksmithIndex = Math.Clamp(menu.BlacksmithIndex, 0, count - 1);
        if (UiTheme.Previous(key)) menu.BlacksmithIndex = (menu.BlacksmithIndex + count - 1) % count;
        if (UiTheme.Next(key)) menu.BlacksmithIndex = (menu.BlacksmithIndex + 1) % count;
        if (UiTheme.Confirm(key) && CanUpgrade(inventory.Backpack[menu.BlacksmithIndex]))
        { menu.PendingUpgrade = inventory.Backpack[menu.BlacksmithIndex]; menu.ConfirmYes = false; }
    }
}
