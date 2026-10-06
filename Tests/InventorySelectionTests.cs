using Godot;
using System;

namespace Abyss.Tests;
internal sealed partial class RegressionSuite
{
    internal void TestInventorySelection()
    {
        var g = new GameSession(new TestHost(), new TestCanvas(), new TestSettings());
        g.AsciiCanvas.Font = GD.Load<Font>("res://Mono.ttf");
        g.Start(501);
        g.MenuState.InventoryIndex = 6 + g.InventoryState.Backpack.Count - 1;
        g.MenuState.ShopSelling = true;
        g.MenuState.PendingTrade = new Offer { Gear = g.InventoryState.Backpack[^1] };
        g.MerchantService.CompleteTrade();
        if (g.MenuState.InventoryIndex >= 6 + g.InventoryState.Backpack.Count)
            throw new Exception("Selling selected item left an invalid inventory cursor.");
        var potion = new Offer { Potion = 0, Quantity = 1 };
        g.MerchantState.MerchantStock.Add(potion);
        g.PlayerState.Gold = 100;
        g.MenuState.ShopSelling = false;
        g.MenuState.PendingTrade = potion;
        g.MerchantService.CompleteTrade();
        g.RunState.Screen = "pause"; g.MenuState.PauseTab = 1;
        g.Transitions.Enabled = true;
        foreach (int stale in new[] { 999, -1, 6 })
        {
            g.MenuState.InventoryIndex = stale;
            g.GameRenderer.Draw(); g.Transitions.Advance(1); g.GameRenderer.Draw();
            if (g.MenuState.InventoryIndex < 0 || g.MenuState.InventoryIndex >= 6 + g.InventoryState.Backpack.Count)
                throw new Exception("Inventory rendering failed to repair a stale cursor.");
            g.MenuState.InventoryIndex = stale;
            g.MenuController.HandleInventory(Key.Down);
            g.GameRenderer.Draw();
        }
        GD.Print("INVENTORY SELECTION AUDIT: selected-item sale, potion purchase, empty backpack, stale cursor rendering and navigation passed.");
    }
}
