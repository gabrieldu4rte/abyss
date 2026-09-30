using Godot;
using System;
using System.Linq;
using static Abyss.Rules.TabletopRules;

namespace Abyss.Tests;
internal sealed partial class RegressionSuite
{
    internal void TestBalance()
    {
        if (ResolveAttack(1, 100, 2).Hit || !ResolveAttack(20, -100, 99).Critical)
            throw new Exception("Natural 1/20 failed");
        if (HitChance(4, 14) != 55 || HitChance(100, 1) != 95 || HitChance(-100, 99) != 5)
            throw new Exception("D20 probability failed");
        if (Modifier(9) != -1 || Modifier(15) != 2 || Modifier(16) != 3)
            throw new Exception("Attribute modifier failed");
        if (RollDamage(new RegressionSuite.FixedRandom(6), new DamageDice(1, 6, 2), true) != 14)
            throw new Exception("Critical dice failed");
        var random = new Random(99);
        int sum = 0;
        for (int i = 0; i < 10000; i++)
        {
            var potion = RollPotion(random);
            if (potion.Total < 2 || potion.Total > 20)
                throw new Exception("2d10 bounds failed");
            sum += potion.Total;
        }

        if (Math.Abs(sum / 10000.0 - 11) > .2)
            throw new Exception("2d10 mean failed");
        for (int c = 0; c < 4; c++)
        {
            game.PlayerState.ClassIndex = c;
            game.Start(77);
            game.DungeonState.Enemies.Clear();
            int oldEnergy = game.PlayerState.Energy, oldTurn = game.RunState.Turn;
            if (!game.HeroCombatStats.CanShoot)
            {
                game.PlayerActions.BeginAim();
                game.PlayerActions.Shoot(Vector2I.Right);
                if (game.RunState.IsAiming || game.PlayerState.Energy != oldEnergy || game.RunState.Turn != oldTurn)
                    throw new Exception("Melee class can shoot");
            }
            else
            {
                int cost = game.HeroCombatStats.ShotCost;
                game.PlayerActions.Shoot(Vector2I.Right);
                if (game.PlayerState.Energy != oldEnergy - cost || game.RunState.Turn != oldTurn + 1)
                    throw new Exception("Shot cost failed");
                game.PlayerState.Energy = 0;
                oldTurn = game.RunState.Turn;
                game.PlayerActions.Shoot(Vector2I.Right);
                if (game.RunState.Turn != oldTurn + 1 || game.PlayerState.Energy != 0)
                    throw new Exception("Free basic shot failed at zero energy");
            }

            game.Start(77);
            game.DungeonState.Enemies.Clear();
            int previousCost = game.HeroCombatStats.AbilityCost;
            double previousDamage = game.HeroCombatStats.AbilityDice.Average;
            for (int n = 2; n <= 8; n++)
            {
                game.ProgressionService.GainXp(game.HeroCombatStats.XpToNext);
                if (game.HeroCombatStats.AbilityCost > previousCost || game.HeroCombatStats.AbilityDice.Average < previousDamage)
                    throw new Exception("Progression regressed");
                previousCost = game.HeroCombatStats.AbilityCost;
                previousDamage = game.HeroCombatStats.AbilityDice.Average;
            }

            game.Start(77);
            game.DungeonState.Enemies.Clear();
            game.PlayerState.Energy = 0;
            oldTurn = game.RunState.Turn;
            game.PlayerActions.Skill();
            if (game.RunState.Turn != oldTurn)
                throw new Exception("Underfunded ability used a turn");
            var pos = GameRules.Directions.Select(d => game.PlayerState.Position + d).First(game.DungeonState.Walk);
            var foe = new Enemy(pos, 'g', 1)
            {
                Health = 999,
                MaxHealth = 999
            };
            game.DungeonState.Enemies.Add(foe);
            game.DungeonGenerator.Reveal();
            game.PlayerState.Energy = game.PlayerState.MaxEnergy;
            game.RandomGenerator = new RegressionSuite.FixedRandom(1);
            int oldHp = foe.Health, costQ = game.HeroCombatStats.AbilityCost;
            game.PlayerActions.Skill();
            if (foe.Health != oldHp || game.PlayerState.Energy != game.PlayerState.MaxEnergy - costQ || game.RunState.Turn != 1)
                throw new Exception("Miss must consume energy and turn");
            if (!game.ExpeditionJournal.Entries.Any(e => e.En.Contains("You miss")))
                throw new Exception("Miss not logged");
            game.DungeonState.Enemies.Clear();
            game.PlayerState.Health = 1;
            game.InventoryState.Potions = 2;
            game.RandomGenerator = new RegressionSuite.FixedRandom(1);
            game.PlayerActions.Drink();
            if (game.ExpeditionJournal.LastPotionRoll != 2 || game.ExpeditionJournal.LastPotionHealing != 2 || game.PlayerState.Health != 3 || game.InventoryState.Potions != 1)
                throw new Exception("Minimum potion failed");
            game.PlayerState.Health = game.PlayerState.MaxHealth - 1;
            game.RandomGenerator = new RegressionSuite.FixedRandom(10);
            game.PlayerActions.Drink();
            if (game.ExpeditionJournal.LastPotionRoll != 20 || game.ExpeditionJournal.LastPotionHealing != 1 || game.PlayerState.Health != game.PlayerState.MaxHealth)
                throw new Exception("Potion clamping failed");
            game.InventoryState.Potions = 1;
            oldTurn = game.RunState.Turn;
            game.PlayerActions.Drink();
            if (game.InventoryState.Potions != 1 || game.RunState.Turn != oldTurn)
                throw new Exception("Full-health potion consumed");
        }

        foreach (char g in new[]
        {
            'r',
            's',
            'g',
            'B'
        }

        )
            for (int depth = 2; depth <= 1000; depth++)
            {
                var old = new Enemy(Vector2I.Zero, g, depth - 1);
                var current = new Enemy(Vector2I.Zero, g, depth);
                if (current.MaxHealth < old.MaxHealth || current.AttackBonus < old.AttackBonus || GameRules.EnemyXp(g, depth) < GameRules.EnemyXp(g, depth - 1))
                    throw new Exception("Enemy growth failed");
                bool newBand = (depth - 1) % 5 == 0;
                if (newBand && (current.MaxHealth <= old.MaxHealth || current.Dice.Average <= old.Dice.Average))
                    throw new Exception("Five-floor strength step missing");
                if (!newBand && (current.MaxHealth != old.MaxHealth || current.Dice != old.Dice || current.Stats != old.Stats || current.AttackBonus != old.AttackBonus || current.Armor != old.Armor))
                    throw new Exception("Enemy stats changed inside a five-floor band");
            }

        foreach (var spec in new[]
        {
            ('r', 4, 2.0),
            ('s', 6, 2.5),
            ('g', 8, 3.5),
            ('B', 24, 5.0)
        }

        )
        {
            var early = new Enemy(Vector2I.Zero, spec.Item1, spec.Item1 == 'B' ? 5 : 1);
            if (early.MaxHealth != spec.Item2 || early.Dice.Average != spec.Item3)
                throw new Exception("Starting enemy tuning changed");
        }

        game.PlayerState.ClassIndex = 0;
        game.Start(77);
        var oldStats = game.PlayerState.Attributes;
        game.PlayerState.Health = 5;
        game.PlayerState.Energy = 0;
        game.ProgressionService.GainXp(30 + 50 + 70 + 5);
        if (game.PlayerState.Level != 4 || game.PlayerState.Experience != 5 || game.PlayerState.Attributes.Strength != oldStats.Strength + 2 || game.PlayerState.Attributes.Constitution != oldStats.Constitution + 1 || game.PlayerState.Health >= game.PlayerState.MaxHealth || game.PlayerState.Energy >= game.PlayerState.MaxEnergy)
            throw new Exception("Slow level progression failed");
        game.DungeonState.Enemies.Clear();
        var target = new Enemy(game.PlayerState.Position + Vector2I.Right, 's', 1)
        {
            Health = 1
        };
        game.DungeonState.Enemies.Add(target);
        game.CombatService.Hit(target, 2);
        if (!game.ExpeditionJournal.Entries.Any(e => e.En.Contains("Skeleton") && e.Pt.Contains("Esqueleto")))
            throw new Exception("Named journal failed");
        for (int seed = 1; seed <= 100; seed++)
        {
            game.Start(seed);
            if (game.InventoryState.Potions != 5 || game.DungeonState.Items.Values.Count(g => g == '!') > 1 || game.DungeonState.Items.Values.Count(g => g == '*') > 1 || game.DungeonState.Items.Values.Count(g => g == '$') != 2 || game.DungeonState.Items.Values.Count(g => g == 'C') > (game.DungeonState.Modifier == FloorModifier.HiddenCache ? 3 : 1))
                throw new Exception("Loot scarcity failed");
        }

        random = new Random(734);
        int hits = 0;
        for (int i = 0; i < 20000; i++)
            if (ResolveAttack(random.Next(1, 21), 4, 14).Hit)
                hits++;
        double measured = hits / 20000.0;
        if (Math.Abs(measured - .55) > .015)
            throw new Exception("Simulated hit rate disagrees with probability");
        GD.Print($"DICE AUDIT: +4 vs AC14 = 55%; observed {measured:P1} over 20,000 attacks. 2d10 mean {sum / 10000.0:F2}.");
    }
}
