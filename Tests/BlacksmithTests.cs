using Godot;
using System;
using System.Linq;
namespace Abyss.Tests;
internal sealed partial class RegressionSuite
{
    internal void TestBlacksmith()
    {
        var test = new GameSession(new TestHost(), new TestCanvas(), new TestSettings());
        int encounters = 0;
        for (int seed = 1; seed <= 600; seed++)
        {
            test.Start(seed);
            test.DungeonState.Floor = 4;
            test.DungeonGenerator.Generate(false);
            var dungeon = test.DungeonState;
            if (!dungeon.BlacksmithRoom.HasValue) continue;
            encounters++;
            if (dungeon.IsSanctuary(dungeon.Stairs) || dungeon.IsSanctuary(test.PlayerState.Position) || dungeon.Enemies.Any(e => dungeon.IsSanctuary(e.Position)) || dungeon.Items.Keys.Any(dungeon.IsSanctuary) || dungeon.Environment.Fixtures.Keys.Any(dungeon.IsSanctuary)) throw new Exception("Unsafe smith generation");
            if (!dungeon.Walk(dungeon.BlacksmithPosition)) throw new Exception("Smith not accessible");
        }
        if (encounters < 5 || encounters > 40) throw new Exception($"Unexpected blacksmith frequency: {encounters}/600");
        test.Start(712);
        test.DungeonState.Floor = 4;
        test.DungeonGenerator.Generate(false, true);
        var world = test.DungeonState;
        var room = world.BlacksmithRoom!.Value;
        test.PlayerState.Position = world.BlacksmithPosition + Vector2I.Down;
        int turn = test.RunState.Turn;
        test.PlayerActions.Move(Vector2I.Up);
        if (test.PlayerState.Position == world.BlacksmithPosition || test.RunState.Turn != turn) throw new Exception("Walked through smith");
        test.PlayerActions.Interact();
        if (test.RunState.Screen != "blacksmith") throw new Exception("Smith interaction failed");
        var weapon = test.InventoryState.Weapon!;
        test.PlayerState.Gold = 1000;
        test.BlacksmithService.Handle(Key.Enter);
        test.BlacksmithService.Handle(Key.Enter);
        if (test.PlayerState.Gold != 1000 || !ReferenceEquals(weapon, test.InventoryState.Weapon)) throw new Exception("Upgrade default must cancel");
        test.BlacksmithService.Handle(Key.Enter);
        test.BlacksmithService.Handle(Key.Down);
        test.BlacksmithService.Handle(Key.Enter);
        if (test.PlayerState.Gold != 940 || test.InventoryState.Backpack[0].Quality != Rarity.Rare || test.InventoryState.Weapon != null) throw new Exception("Upgrade payment or unequip failed");
        test.BlacksmithService.CompleteUpgrade();
        if (test.PlayerState.Gold != 940) throw new Exception("Upgrade applied twice");
        test.PlayerState.Level = 30;
        test.InventoryState.Equipped[0] = test.InventoryState.Backpack[0];
        foreach (var rarity in new[] { Rarity.Epic, Rarity.Legendary })
        {
            var old = test.InventoryState.Backpack[0];
            int money = test.PlayerState.Gold;
            test.MenuState.PendingUpgrade = old;
            test.BlacksmithService.CompleteUpgrade();
            if (test.PlayerState.Gold != money - BlacksmithService.Price(old) || test.InventoryState.Weapon?.Quality != rarity || !ReferenceEquals(test.InventoryState.Weapon, test.InventoryState.Backpack[0])) throw new Exception("Equipped upgrade identity failed");
        }
        foreach (var id in Enum.GetValues<ItemId>().Where(id => id != ItemId.None))
            if (BlacksmithService.CanUpgrade(NamedItemCatalog.Create(id))) throw new Exception("Unique equipment can be upgraded");
        if (BlacksmithService.CanUpgrade(test.InventoryState.Weapon!)) throw new Exception("Legendary can upgrade");
        var common = new Gear(GearKind.Sword, Rarity.Common);
        test.InventoryState.Backpack.Add(common);
        test.PlayerState.Gold = 0;
        test.MenuState.PendingUpgrade = common;
        test.BlacksmithService.CompleteUpgrade();
        if (!test.InventoryState.Backpack.Any(g => ReferenceEquals(g, common))) throw new Exception("Free upgrade");
        test.PlayerState.Gold = 1000;
        test.MenuState.PendingUpgrade = new Gear(GearKind.Sword, Rarity.Common);
        test.BlacksmithService.CompleteUpgrade();
        if (test.PlayerState.Gold != 1000) throw new Exception("Upgrade matched value instead of identity");
        world.Enemies.Clear();
        var border = Enumerable.Range(room.Position.X, room.Size.X).Select(x => new Vector2I(x, room.Position.Y)).Concat(Enumerable.Range(room.Position.Y, room.Size.Y).Select(y => new Vector2I(room.Position.X, y))).First(p => GameRules.Directions.Any(d => world.Walk(p + d) && !world.IsSanctuary(p + d)));
        var outside = GameRules.Directions.Select(d => border + d).First(p => world.Walk(p) && !world.IsSanctuary(p));
        var enemy = new Enemy(outside, 's', 4);
        world.Enemies.Add(enemy);
        test.PlayerState.Position = border;
        test.EnemyNavigator.StepEnemy(enemy, world.BlacksmithPosition);
        if (world.IsSanctuary(enemy.Position)) throw new Exception("Enemy entered sanctuary");
        int hp = test.PlayerState.Health;
        test.CombatService.ResolveEnemyAttack(enemy, false);
        if (test.PlayerState.Health != hp) throw new Exception("Enemy hit into sanctuary");
        test.PlayerState.Position = world.BlacksmithPosition + Vector2I.Down;
        EnemyDisplacement.Push(world, test.PlayerState, enemy, outside + (outside - border));
        if (world.IsSanctuary(enemy.Position)) throw new Exception("Enemy pushed into sanctuary");
        test.EnvironmentService.Ignite(border);
        if (world.Environment.Fire.ContainsKey(border)) throw new Exception("Sanctuary burned");
        test.DungeonGenerator.Generate(true);
        test.MenuState.ShopSelling = true;
        for (int i = 0; i < 12; i++)
        {
            var gear = new Gear(GearKind.Dagger, Rarity.Common);
            test.InventoryState.Backpack.Add(gear);
            test.MenuState.PendingTrade = new Offer { Gear = gear };
            test.MerchantService.CompleteTrade();
            if (test.MerchantState.MerchantStock.Count(o => o.Quantity > 0) > 5) throw new Exception("Resale exceeded stock limit");
        }
        if (world.BlacksmithRoom.HasValue) throw new Exception("Smith room persisted onto merchant floor");
        GD.Print($"BLACKSMITH AUDIT: {encounters}/600 encounters; sanctuary, interaction, confirmations, rarity costs, equipped identity, unique exclusion and five-offer cap passed.");
    }
}
