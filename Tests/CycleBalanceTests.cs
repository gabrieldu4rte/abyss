using Godot;
using System;
using System.Linq;
using static Abyss.Rules.TabletopRules;

namespace Abyss.Tests;
internal sealed partial class RegressionSuite
{
    internal void TestCycleBalance()
    {
        game.PlayerState.ClassIndex = 0;
        game.Start(77);
        game.DungeonState.Enemies.Clear();
        game.PlayerState.Attributes = new Attributes(20, 12, 14, 8);
        game.InventoryState.Equipped[0] = new Gear(GearKind.Sword, Rarity.Common);
        if (game.HeroCombatStats.MeleeModifier != 5 || game.HeroCombatStats.MeleeBonus != game.HeroCombatStats.Proficiency + 5 || game.HeroCombatStats.MeleeDice.Bonus != 5)
            throw new Exception("Sword did not use STR for accuracy and damage");
        game.InventoryState.Equipped[0] = new Gear(GearKind.Dagger, Rarity.Common);
        if (game.HeroCombatStats.MeleeModifier != 1 || game.HeroCombatStats.MeleeDice.Bonus != 1 || game.HeroCombatStats.PrimaryModifier != 5)
            throw new Exception("Finesse weapon did not use DEX independently of warrior ability");
        game.PlayerState.ClassIndex = 3;
        game.InventoryState.Equipped[0] = new Gear(GearKind.Sword, Rarity.Common);
        if (game.HeroCombatStats.MeleeModifier != 5 || game.HeroCombatStats.PrimaryModifier != 1 || game.HeroCombatStats.AbilityDice.Bonus != 1)
            throw new Exception("Rogue sword and skill attributes conflated");
        game.PlayerState.ClassIndex = 1;
        game.InventoryState.Equipped[0] = new Gear(GearKind.Staff, Rarity.Common);
        if (game.HeroCombatStats.SpellBonus != game.HeroCombatStats.Proficiency - 1 || game.HeroCombatStats.ShotDice.Bonus != -1 || game.HeroCombatStats.AbilityDice.Bonus != -1)
            throw new Exception("Mage INT accuracy/damage failed");
        game.PlayerState.ClassIndex = 2;
        game.InventoryState.Equipped[0] = new Gear(GearKind.Bow, Rarity.Common);
        if (game.HeroCombatStats.SpellBonus != game.HeroCombatStats.Proficiency + 1 || game.HeroCombatStats.ShotDice.Bonus != 1 || game.HeroCombatStats.AbilityDice.Bonus != 1)
            throw new Exception("Archer DEX accuracy/damage failed");
        var foe = new Enemy(game.PlayerState.Position + Vector2I.Right, 'g', 1);
        foreach (int hero in Enumerable.Range(0, 4))
        {
            game.PlayerState.ClassIndex = hero;
            if (game.CombatService.TargetDefense(foe, false, false) != foe.Armor || game.CombatService.TargetDefense(foe, true, false) != foe.Armor || game.CombatService.TargetDefense(foe, false, true) != foe.Armor)
                throw new Exception("Physical and magic attacks use different defenses");
        }

        game.PlayerState.ClassIndex = 0;
        game.InventoryState.Equipped[0] = null;
        if (game.HeroCombatStats.Defense != 12)
            throw new Exception("Unarmored defense is not 10 + CON modifier");
        int saved = game.HeroCombatStats.Defense;
        game.PlayerState.Attributes = game.PlayerState.Attributes with
        {
            Dexterity = 30,
            Intelligence = 30
        };
        if (game.HeroCombatStats.Defense != saved)
            throw new Exception("DEX or INT still changes defense");
        foreach (GearKind kind in new[]
        {
            GearKind.Plate,
            GearKind.Leather,
            GearKind.Robe
        }

        )
        {
            var armor = new Gear(kind, Rarity.Rare);
            game.InventoryState.Equipped[1] = armor;
            if (game.HeroCombatStats.Defense != armor.ArmorClass + game.HeroCombatStats.EffectiveAttributes.Con)
                throw new Exception("Armor class + CON formula failed");
        }

        game.InventoryState.Equipped[1] = null;
        var baseStats = game.PlayerState.Attributes;
        game.InventoryState.Equipped[2] = new Gear(GearKind.Amulet, Rarity.Rare);
        if (game.HeroCombatStats.EffectiveAttributes.Strength != baseStats.Strength + 2 || game.HeroCombatStats.EffectiveAttributes.Constitution != baseStats.Constitution + 2)
            throw new Exception("Accessory attribute bonus missing");
        game.InventoryState.Equipped[2] = null;
        if (game.HeroCombatStats.EffectiveAttributes != baseStats)
            throw new Exception("Removed equipment left attribute bonuses");
        foreach (char glyph in new[]
        {
            'r',
            's',
            'g',
            'B'
        }

        )
        {
            var e = new Enemy(Vector2I.Zero, glyph, 1);
            int attack = e.AttackBonus, damage = e.Dice.Bonus, def = e.Armor;
            e.Stats = glyph is 'r' or 'g' ? e.Stats with
            {
                Dexterity = e.Stats.Dexterity + 2
            }

            : e.Stats with
            {
                Strength = e.Stats.Strength + 2
            };
            if (e.AttackBonus != attack + 1 || e.Dice.Bonus != damage + 1 || e.Armor != def)
                throw new Exception("Enemy attack attribute does not affect both accuracy and damage");
            e.Stats = e.Stats with
            {
                Constitution = e.Stats.Constitution + 2
            };
            if (e.Armor != def + 1)
                throw new Exception("Enemy CON defense failed");
        }

        if (GameRules.ChestChance(1) <= .35 || GameRules.ChestChance(6) <= GameRules.ChestChance(5) || GameRules.ExtraChestDropChance(6) <= GameRules.ExtraChestDropChance(1))
            throw new Exception("Chest cycle increase missing");
        for (int depth = 1; depth <= 100; depth++)
        {
            if (GameRules.ChestChance(depth) > .60 || GameRules.ExtraChestDropChance(depth) > .35)
                throw new Exception("Chest rewards exceed caps");
            for (int roll = 0; roll < 100; roll++)
            {
                game.RandomGenerator = new RegressionSuite.RangeRandom(roll);
                Rarity early = game.LootService.RollRarity(depth);
                game.RandomGenerator = new RegressionSuite.RangeRandom(roll);
                Rarity later = game.LootService.RollRarity(depth + 5);
                if (later < early)
                    throw new Exception("Chest rarity regressed between cycles");
                game.RandomGenerator = new RegressionSuite.RangeRandom(roll);
                early = game.LootService.RollRarity(depth, true);
                game.RandomGenerator = new RegressionSuite.RangeRandom(roll);
                later = game.LootService.RollRarity(depth + 5, true);
                if (early < Rarity.Rare || later < early)
                    throw new Exception("Guardian rarity regressed or fell below rare");
            }
        }

        foreach (int depth in new[]
        {
            5,
            10,
            25,
            100
        }

        )
        {
            game.PlayerState.ClassIndex = 0;
            game.Start(depth);
            game.DungeonState.Enemies.Clear();
            game.DungeonState.Floor = depth;
            game.RandomGenerator = new Random(depth);
            var boss = new Enemy(game.PlayerState.Position + Vector2I.Right, 'B', depth);
            game.DungeonState.Enemies.Add(boss);
            int money = game.PlayerState.Gold, count = game.InventoryState.Backpack.Count;
            game.CombatService.Hit(boss, boss.Health);
            int cycle = GameRules.CycleIndex(depth), reward = game.PlayerState.Gold - money;
            if (reward < 18 + cycle * 8 || reward > 28 + cycle * 8 || game.InventoryState.Backpack.Count != count + 1 || game.InventoryState.Backpack[^1].Quality < Rarity.Rare)
                throw new Exception("Guardian guaranteed reward failed");
            for (int i = 0; i < 500; i++)
            {
                int gold = game.LootService.RollEnemyGold('r', depth);
                if (gold != 0 && (gold < 1 + cycle || gold > 2 + cycle * 2))
                    throw new Exception("Normal enemy gold outside cycle range");
            }
        }

        GD.Print("CYCLE AUDIT: weapon/class attributes, unified CON defense, reversible gear bonuses, rarity progression, chest caps and guaranteed guardian loot passed.");
    }
}
