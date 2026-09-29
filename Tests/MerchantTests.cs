using Godot;
using System;
using System.Linq;

namespace Abyss.Tests;
internal sealed partial class RegressionSuite
{
    internal void TestMerchant()
    {
        game.PlayerState.ClassIndex = 0;
        game.Start(91);
        game.DungeonState.Floor = 16;
        game.DungeonGenerator.Generate(true);
        if (!game.DungeonState.IsMerchantFloor || game.DungeonState.Enemies.Count != 0 || game.DungeonState.Items.Count != 0 || !game.DungeonState.Walk(game.DungeonState.Stairs) || !game.DungeonState.Walk(game.DungeonState.MerchantPosition) || game.MerchantState.MerchantStock.Count != 9)
            throw new Exception("Unsafe or incomplete merchant room");
        var originalStock = game.MerchantState.MerchantStock.ToArray();
        var originalPlayer = game.PlayerState.Position;
        int originalTurn = game.RunState.Turn;
        game.PlayerState.Position = game.DungeonState.MerchantPosition + Vector2I.Down;
        game.PlayerActions.Move(Vector2I.Up);
        if (game.PlayerState.Position == game.DungeonState.MerchantPosition || game.RunState.Turn != originalTurn)
            throw new Exception("Walked through merchant");
        game.PlayerActions.Interact();
        if (game.RunState.Screen != "shop")
            throw new Exception("Merchant interaction failed");
        game.MenuController.HandleShop(Key.Escape);
        game.PlayerActions.Interact();
        if (!originalStock.SequenceEqual(game.MerchantState.MerchantStock))
            throw new Exception("Reopening shop rerolled stock");
        game.PlayerState.Gold = 10000;
        int money = game.PlayerState.Gold, count = game.InventoryState.Backpack.Count;
        game.MenuController.HandleShop(Key.Enter);
        if (game.MenuState.PendingTrade == null || game.MenuState.ConfirmYes || game.PlayerState.Gold != money || game.InventoryState.Backpack.Count != count)
            throw new Exception("Purchase did not require explicit confirmation");
        game.MenuController.HandleShop(Key.Enter);
        if (game.MenuState.PendingTrade != null || game.PlayerState.Gold != money || game.InventoryState.Backpack.Count != count)
            throw new Exception("Default purchase choice is not cancel");
        game.MenuController.HandleShop(Key.Enter);
        var bought = game.MenuState.PendingTrade!;
        int price = bought.Value;
        game.MenuController.HandleShop(Key.Down);
        game.MenuController.HandleShop(Key.Enter);
        if (game.PlayerState.Gold != money - price || game.InventoryState.Backpack.Count != count + 1 || bought.Quantity != 0 || !ReferenceEquals(game.InventoryState.Backpack[^1], bought.Gear))
            throw new Exception("Purchase failed");
        money = game.PlayerState.Gold;
        game.MerchantService.CompleteTrade();
        if (game.PlayerState.Gold != money)
            throw new Exception("Transaction applied twice");
        game.PlayerState.Gold = 0;
        count = game.InventoryState.Backpack.Count;
        game.MenuState.ShopIndex = 0;
        game.MenuController.HandleShop(Key.Enter);
        game.MenuController.HandleShop(Key.Down);
        game.MenuController.HandleShop(Key.Enter);
        if (game.PlayerState.Gold != 0 || game.InventoryState.Backpack.Count != count)
            throw new Exception("Purchase allowed without gold");
        game.MenuState.ShopSelling = true;
        game.MenuState.ShopIndex = 0;
        var weapon = game.InventoryState.Weapon!;
        game.MenuController.HandleShop(Key.Enter);
        game.MenuController.HandleShop(Key.Escape);
        if (!ReferenceEquals(game.InventoryState.Weapon, weapon) || !game.InventoryState.Backpack.Contains(weapon))
            throw new Exception("Cancelled sale removed equipped item");
        game.MenuController.HandleShop(Key.Enter);
        game.MenuController.HandleShop(Key.Down);
        game.MenuController.HandleShop(Key.Enter);
        if (game.InventoryState.Weapon != null || game.InventoryState.Backpack.Any(g => ReferenceEquals(g, weapon)) || game.PlayerState.Gold != weapon.Value / 2)
            throw new Exception("Equipped sale failed");
        var saleList = game.MerchantService.ShopOffers();
        game.MenuState.ShopIndex = saleList.FindIndex(o => o.Gear == null && o.Potion == 0);
        int beforePotions = game.InventoryState.Potions;
        money = game.PlayerState.Gold;
        game.MenuController.HandleShop(Key.Enter);
        game.MenuController.HandleShop(Key.Down);
        game.MenuController.HandleShop(Key.Enter);
        if (game.InventoryState.Potions != beforePotions - 1 || game.PlayerState.Gold != money + 6)
            throw new Exception("Potion sale did not transfer exactly one");
        game.MenuState.ShopSelling = false;
        game.MenuState.ShopIndex = game.MerchantService.ShopOffers().FindIndex(o => o.Gear == null && o.Potion == 0);
        game.PlayerState.Gold = 100;
        beforePotions = game.InventoryState.Potions;
        game.MenuController.HandleShop(Key.Enter);
        game.MenuController.HandleShop(Key.Down);
        game.MenuController.HandleShop(Key.Enter);
        if (game.InventoryState.Potions != beforePotions + 1 || game.PlayerState.Gold != 88)
            throw new Exception("Potion purchase failed");
        for (int i = 0; i < 30; i++)
            game.EndTurn();
        if (game.DungeonState.Enemies.Count != 0)
            throw new Exception("Enemies entered safe room");
        game.PlayerState.Position = game.DungeonState.Stairs;
        game.RunState.Screen = "game";
        int oldFloor = game.DungeonState.Floor;
        game.PlayerActions.Interact();
        if (game.DungeonState.Floor != oldFloor + 1)
            throw new Exception("Safe-room stairs failed");
        game.RunState.Screen = "pause";
        game.MenuState.PauseTab = 4;
        int savedTurn = game.RunState.Turn;
        money = game.PlayerState.Gold;
        game.MenuController.AskReturnToMenu();
        game.MenuController.HandleExitConfirm(Key.Enter);
        if (game.RunState.Screen != "pause" || game.PlayerState.Gold != money || game.RunState.Turn != savedTurn)
            throw new Exception("Default exit did not preserve run");
        game.MenuController.AskReturnToMenu();
        game.MenuController.HandleExitConfirm(Key.Escape);
        if (game.RunState.Screen != "pause")
            throw new Exception("Exit escape failed");
        game.MenuController.AskReturnToMenu();
        game.MenuController.HandleExitConfirm(Key.Down);
        game.MenuController.HandleExitConfirm(Key.Enter);
        if (game.RunState.Screen != "home")
            throw new Exception("Confirmed exit failed");
        int encounters = 0;
        for (int seed = 1; seed <= 200; seed++)
        {
            game.Start(seed);
            if (game.DungeonState.IsMerchantFloor)
                throw new Exception("Starting floor became merchant floor");
            game.DungeonState.Floor = 4;
            game.DungeonGenerator.Generate();
            if (game.DungeonState.IsMerchantFloor)
            {
                encounters++;
                if (game.DungeonState.Enemies.Count != 0 || game.DungeonState.Items.Count != 0)
                    throw new Exception("Unsafe random encounter");
            }

            game.DungeonState.Floor = 5;
            game.DungeonGenerator.Generate();
            if (game.DungeonState.IsMerchantFloor || game.DungeonState.Enemies.All(e => e.Glyph != 'B'))
                throw new Exception("Merchant replaced boss floor");
        }

        if (encounters < 8 || encounters > 35)
            throw new Exception("Merchant occurrence rate outside expected sample range");
        GD.Print($"MERCHANT AUDIT: {encounters}/200 eligible encounters; safe rooms, persistent stock, purchase/sale confirmation, currency, equipped sales, potions, exit cancellation passed.");
    }
}
