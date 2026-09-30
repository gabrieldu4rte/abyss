using Godot;
using System;
using System.Linq;

namespace Abyss.Tests;
internal sealed partial class RegressionSuite
{
    internal void TestInventory()
    {
        for (int hero = 0; hero < 4; hero++)
        {
            game.PlayerState.ClassIndex = hero;
            game.Start(321);
            game.DungeonState.Enemies.Clear();
            if (game.InventoryState.Backpack.Count != 1 || game.InventoryState.Weapon?.Quality != Rarity.Common || game.InventoryState.Equipped[1] != null || game.InventoryState.Equipped[2] != null || game.InventoryState.Potions != 5 || game.InventoryState.EnergyPotions != 0)
                throw new Exception("Starting inventory incorrect");
            var weapon = game.InventoryState.Weapon!;
            double armed = game.HeroCombatStats.MeleeDice.Average;
            game.InventoryService.ToggleGear(weapon);
            if (game.InventoryState.Weapon != null || game.HeroCombatStats.MeleeDice.Average >= armed || game.HeroCombatStats.CanShoot)
                throw new Exception("Unarmed damage or ranged restriction failed");
            game.InventoryService.ToggleGear(weapon);
            if (hero is 1 or 2)
            {
                game.PlayerState.Energy = 0;
                int before = game.RunState.Turn;
                game.PlayerActions.Shoot(Vector2I.Right);
                if (game.PlayerState.Energy != 0 || game.RunState.Turn != before + 1 || game.HeroCombatStats.ShotDice.Sides != weapon.Sides)
                    throw new Exception("Weapon-based free ranged attack failed");
                var delta = GameRules.Directions.First(d => game.DungeonState.Walk(game.PlayerState.Position + d));
                var rangedTarget = new Enemy(game.PlayerState.Position + delta, 'r', 1)
                {
                    Health = 999,
                    MaxHealth = 999
                };
                game.DungeonState.Enemies.Add(rangedTarget);
                game.RandomGenerator = new RegressionSuite.FixedRandom(20);
                before = game.RunState.Turn;
                game.PlayerActions.Shoot(delta);
                if (rangedTarget.Health != 999 - (weapon.Sides * 2 + game.HeroCombatStats.PrimaryModifier) || game.PlayerState.Energy != 0 || game.RunState.Turn != before + 1)
                    throw new Exception("Ranged weapon damage did not reach target");
                game.DungeonState.Enemies.Clear();
            }

            foreach (GearKind kind in Enum.GetValues<GearKind>())
            {
                var gear = new Gear(kind, Rarity.Common);
                game.InventoryState.Backpack.Add(gear);
                int before = game.RunState.Turn;
                game.InventoryService.ToggleGear(gear);
                if (gear.Allows(hero) ? !ReferenceEquals(game.InventoryState.Equipped[(int)gear.Slot], gear) : game.RunState.Turn != before)
                    throw new Exception("Class equipment restriction failed");
            }
        }

        game.PlayerState.ClassIndex = 0;
        game.Start(77);
        game.DungeonState.Enemies.Clear();
        var legendary = new Gear(GearKind.Sword, Rarity.Legendary);
        game.InventoryState.Backpack.Add(legendary);
        int oldTurn = game.RunState.Turn;
        game.InventoryService.ToggleGear(legendary);
        if (game.RunState.Turn != oldTurn || ReferenceEquals(game.InventoryState.Weapon, legendary))
            throw new Exception("Level requirement bypassed");
        game.PlayerState.Level = 10;
        game.InventoryService.ToggleGear(legendary);
        game.PlayerState.Health = game.PlayerState.MaxHealth - 10;
        var enemy = new Enemy(game.PlayerState.Position + Vector2I.Right, 'r', 1)
        {
            Health = 999,
            MaxHealth = 999
        };
        game.RandomGenerator = new RegressionSuite.FixedRandom(20);
        int oldHp = game.PlayerState.Health;
        game.CombatService.ResolveHeroAttack(enemy);
        if (game.PlayerState.Health != oldHp + 2 || enemy.Health != 999 - (legendary.Sides * 2 + game.PlayerState.Attributes.Str + legendary.Power + 6))
            throw new Exception("Legendary impact/drain failed");
        var epic = new Gear(GearKind.Sword, Rarity.Epic);
        game.InventoryState.Backpack.Add(epic);
        game.InventoryService.ToggleGear(epic);
        oldHp = game.PlayerState.Health;
        int enemyHp = enemy.Health;
        game.CombatService.ResolveHeroAttack(enemy);
        if (game.PlayerState.Health != oldHp || enemy.Health != enemyHp - (epic.Sides * 2 + game.PlayerState.Attributes.Str + epic.Power + 4))
            throw new Exception("Epic impact failed");
        int oldDefense = game.HeroCombatStats.Defense;
        var armor = new Gear(GearKind.Plate, Rarity.Legendary);
        game.InventoryState.Backpack.Add(armor);
        game.InventoryService.ToggleGear(armor);
        if (game.HeroCombatStats.Defense <= oldDefense || game.InventoryState.DamageReduction != 2)
            throw new Exception("Armor bonuses failed");
        game.PlayerState.Health = game.PlayerState.MaxHealth;
        int full = game.PlayerState.Health;
        game.CombatService.ResolveEnemyAttack(enemy, false);
        if (game.PlayerState.Health != full - Math.Max(1, TabletopRules.RollDamage(new RegressionSuite.FixedRandom(20), enemy.Dice, true) - 2))
            throw new Exception("Armor reduction not applied to actual damage");
        game.InventoryService.ToggleGear(armor);
        if (game.HeroCombatStats.Defense != oldDefense || game.InventoryState.DamageReduction != 0)
            throw new Exception("Removed armor retained bonuses");
        var charm = new Gear(GearKind.Amulet, Rarity.Epic);
        game.InventoryState.Backpack.Add(charm);
        game.InventoryService.ToggleGear(charm);
        game.PlayerState.Energy = 0;
        var victim = new Enemy(game.PlayerState.Position + Vector2I.Right, 'r', 1)
        {
            Health = 1
        };
        game.DungeonState.Enemies.Add(victim);
        game.CombatService.Hit(victim, 2);
        if (game.PlayerState.Energy != 1 || game.HeroCombatStats.EffectiveAttributes.Strength != game.PlayerState.Attributes.Strength + 3)
            throw new Exception("Accessory effects failed");
        game.RandomGenerator = new Random(123);
        for (int i = 0; i < 1000; i++)
            if (game.LootService.RollRarity(1) > Rarity.Rare)
                throw new Exception("Early rarity gate failed");
        var qualities = Enumerable.Range(0, 2000).Select(_ => game.LootService.RollRarity(100)).Distinct().Count();
        if (qualities != 4)
            throw new Exception("Deep rarity pool incomplete");
        foreach (GearKind kind in Enum.GetValues<GearKind>())
        {
            int value = 0, power = -1;
            foreach (Rarity quality in Enum.GetValues<Rarity>())
            {
                var gear = new Gear(kind, quality);
                if (gear.Value <= value || gear.Power <= power)
                    throw new Exception("Rarity scaling failed");
                value = gear.Value;
                power = gear.Power;
            }
        }

        game.Start(77);
        game.DungeonState.Enemies.Clear();
        game.DungeonState.Items.Clear();
        game.DungeonState.Items[game.PlayerState.Position] = 'C';
        int beforeCount = game.InventoryState.Backpack.Count;
        game.RandomGenerator = new RegressionSuite.FixedRandom(1);
        game.PlayerActions.Pickup();
        int after = game.InventoryState.Backpack.Count;
        game.PlayerActions.Pickup();
        if (after != beforeCount + 1 || game.InventoryState.Backpack.Count != after || game.DungeonState.Items.ContainsKey(game.PlayerState.Position))
            throw new Exception("Chest opened twice or lost loot");
        game.PlayerState.Energy = 0;
        game.InventoryState.EnergyPotions = 1;
        game.RandomGenerator = new RegressionSuite.FixedRandom(1);
        game.InventoryService.DrinkEnergy();
        if (game.InventoryState.EnergyPotions != 0 || game.PlayerState.Energy != 2)
            throw new Exception("Energy potion failed");
        game.RunState.Screen = "game";
        game.GameInput.HandleKey(Key.I);
        if (game.RunState.Screen != "pause" || game.MenuState.PauseTab != 1)
            throw new Exception("Inventory shortcut failed");
        for (int i = 0; i < 20; i++)
            game.InventoryState.Backpack.Add(new Gear(GearKind.Dagger, Rarity.Common));
        for (int i = 0; i < game.InventoryState.Backpack.Count + 6; i++)
            game.GameInput.HandleKey(Key.Down);
        if (game.MenuState.InventoryIndex != 0)
            throw new Exception("Inventory navigation wrap failed");
        GD.Print("INVENTORY AUDIT: starting gear, slots, class/level restrictions, unarmed/ranged damage, rarity effects, chest uniqueness, consumables and navigation passed.");
    }
}
