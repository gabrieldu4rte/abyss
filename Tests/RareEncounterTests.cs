using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Abyss.Tests;
internal sealed partial class RegressionSuite
{
    internal void TestRareEncounters()
    {
        var g = new GameSession(new TestHost(), new TestCanvas(), new TestSettings());
        var dungeon = g.DungeonState;
        var world = dungeon.Environment;
        void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
        void Room()
        {
            g.Start(731); dungeon.Enemies.Clear(); dungeon.Items.Clear(); world.Clear(); dungeon.Modifier = FloorModifier.None;
            g.PlayerState.Position = new Vector2I(20, 13); dungeon.Stairs = new Vector2I(60, 25);
            for (int x = 1; x < GameRules.Width - 1; x++)
                for (int y = 1; y < GameRules.Height - 1; y++) dungeon.Tiles[x, y] = '.';
            g.DungeonGenerator.Reveal();
        }
        int elites = 0, modifiers = 0;
        var seenModifiers = new HashSet<FloorModifier>();
        var seenTitles = new HashSet<EliteTitle>();
        string Signature() => $"{dungeon.Modifier}:" + string.Join(";", dungeon.Enemies.Select(e => $"{e.Position}:{e.Glyph}:{e.MaxHealth}:{string.Join(',', e.Titles)}")) + string.Join(";", dungeon.Items);
        for (int seed = 0; seed < 800; seed++)
        {
            g.Start(seed); dungeon.Floor = 7; g.DungeonGenerator.Generate(false);
            int count = dungeon.Enemies.Count(e => e.IsElite);
            Assert(count <= 1, "More than one elite spawned on one floor.");
            elites += count;
            foreach (var elite in dungeon.Enemies.Where(e => e.IsElite))
            {
                var ordinary = new Enemy(elite.Position, elite.Glyph, elite.Depth);
                Assert(elite.MaxHealth > ordinary.MaxHealth && elite.AttackBonus == ordinary.AttackBonus + 1 && elite.Armor == ordinary.Armor + 1 && elite.Dice.Bonus == ordinary.Dice.Bonus + 1, "Elite stats do not match their modest bonuses.");
                Assert(elite.Titles.Length is 1 or 2 && elite.Titles.Distinct().Count() == elite.Titles.Length, "Elite titles repeat or exceed the label budget.");
                foreach (var title in elite.Titles) seenTitles.Add(title);
            }
            if (dungeon.Modifier != FloorModifier.None) { modifiers++; seenModifiers.Add(dungeon.Modifier); }
            if (dungeon.Modifier == FloorModifier.Infestation) Assert(dungeon.Enemies.Select(e => e.Glyph).Distinct().Count() == 1, "Infestation has mixed enemy types.");
            if (seed < 30)
            {
                string first = Signature();
                g.Start(seed); dungeon.Floor = 7; g.DungeonGenerator.Generate(false);
                Assert(first == Signature(), "Rare floor generation is not deterministic.");
            }
        }
        Assert(elites is > 20 and < 120 && modifiers is > 40 and < 160, "Rare events occur too often or never occur.");
        Assert(seenModifiers.Count == 5 && seenTitles.Count == 6, "Rare event/title variety is unreachable.");
        Assert(Enum.GetValues<EliteTitle>().Select(FloorEventText.TitleColor).Distinct().Count() == 6, "Elite title colors are not distinct.");
        Room();
        var p = g.PlayerState.Position;
        world.Fixtures[p] = Fixture.SpikeTrap;
        int hp = g.PlayerState.Health;
        g.EnvironmentService.Tick();
        Assert(g.PlayerState.Health == hp - 5 && world.HeroPoisonTurns == 0 && world.Fixtures[p] == Fixture.SpentTrap, "Ruin spikes are not an immediate single-use hit.");
        g.EnvironmentService.Tick(); Assert(g.PlayerState.Health == hp - 5, "Spent spikes triggered again.");
        Room();
        var enemy = new Enemy(p + Vector2I.Right, 'g', 1) { Health = 20, MaxHealth = 20 };
        dungeon.Enemies.Add(enemy); world.Fixtures[p] = Fixture.ShockTrap; hp = g.PlayerState.Health;
        g.EnvironmentService.Tick();
        Assert(g.PlayerState.Health == hp - 3 && enemy.Health == 17, "Cistern discharge missed its area targets.");
        Assert(world.Fire.Count == 0 && world.HeroPoisonTurns == 0, "Discharge reused a poison/fire effect.");
        Room(); world.Fixtures[p] = Fixture.FlameTrap; hp = g.PlayerState.Health;
        g.EnvironmentService.Tick();
        Assert(world.Fire.Count == 5 && g.PlayerState.Health == hp - 3 && world.Fire.Values.All(v => v == 3), "Forge flame jet did not create a four-turn cross of fire.");
        Room(); world.Fixtures[p] = Fixture.PoisonTrap; hp = g.PlayerState.Health;
        g.EnvironmentService.Tick(); g.EnvironmentService.Tick(); g.EnvironmentService.Tick();
        Assert(g.PlayerState.Health == hp - 6 && world.HeroPoisonTurns == 0 && world.Fire.Count == 0, "Fungal spores lost their poison behavior.");
        foreach (var trap in new[] { Fixture.SpikeTrap, Fixture.ShockTrap, Fixture.FlameTrap, Fixture.PoisonTrap })
        {
            Room(); g.PlayerState.Health = 1; world.Fixtures[p] = trap; g.EnvironmentService.Tick();
            Assert(g.PlayerState.Health == 0 && g.RunState.Screen == "dead", "Lethal biome trap did not end the run.");
            Room(); enemy = new Enemy(p + Vector2I.Right * 3, 'r', 1) { Health = 20, MaxHealth = 20 }; dungeon.Enemies.Add(enemy);
            world.Fixtures[enemy.Position] = trap; g.EnvironmentService.Tick();
            Assert(enemy.Health < 20 && world.Fixtures[enemy.Position] == Fixture.SpentTrap, "Enemy did not trigger biome trap.");
        }
        Room(); g.FloorEventGenerator.Generate(FloorModifier.Blackout, false);
        world.Fixtures[p + Vector2I.Right * 5] = Fixture.WallTorch; world.Fire[p + Vector2I.Right * 4] = 4;
        g.DungeonGenerator.Reveal();
        Assert(g.InventoryState.HasLight && dungeon.Visible[p.X + 2, p.Y] && !dungeon.Visible[p.X + 3, p.Y] && !dungeon.Visible[p.X + 5, p.Y], "Blackout permits torch/fire illumination.");
        Room(); g.FloorEventGenerator.Generate(FloorModifier.HotDraft, false); g.EnvironmentService.Ignite(p + Vector2I.Right * 3);
        Assert(world.Fire.Values.Single() == 6, "Hot draft did not extend fire.");
        for (int i = 0; i < 6; i++) g.EnvironmentService.Tick();
        Assert(world.Fire.Count == 0, "Hot draft fire never expires.");
        Room(); g.FloorEventGenerator.Generate(FloorModifier.ThinAir, false); g.PlayerState.Energy = 0;
        for (int i = 0; i < 6; i++) g.EndTurn();
        Assert(g.PlayerState.Energy == 0, "Thin air did not slow natural recovery.");
        for (int i = 0; i < 6; i++) g.EndTurn();
        Assert(g.PlayerState.Energy == 1, "Thin air prevented all recovery.");
        Room(); g.FloorEventGenerator.Generate(FloorModifier.HiddenCache, false);
        Assert(dungeon.Items.Count(kv => kv.Value == 'C') == 2 && dungeon.Items.Keys.All(q => q != p && q != dungeon.Stairs && dungeon.Walk(q)), "Hidden caches overlap protected positions.");
        for (int i = 0; i < 50; i++)
        {
            g.Start(i); dungeon.Floor = 10; g.DungeonGenerator.Generate(false);
            Assert(dungeon.Enemies.Count(e => e.Glyph == 'B') == 1 && !dungeon.Enemies.Single(e => e.Glyph == 'B').IsElite && dungeon.Modifier != FloorModifier.Infestation, "Rare generation changed the guardian encounter.");
        }
        g.DungeonGenerator.Generate(true); g.FloorEventGenerator.Generate(FloorModifier.Blackout, true);
        Assert(dungeon.Modifier == FloorModifier.None && dungeon.Enemies.Count == 0, "Rare encounter leaked into merchant refuge.");
        foreach (bool reward in new[] { false, true })
        {
            Room(); enemy = new Enemy(p + Vector2I.Right, 'g', 1); enemy.PromoteElite(EliteTitle.Cruel, EliteTitle.Ancient); dungeon.Enemies.Add(enemy);
            int items = g.InventoryState.Backpack.Count;
            g.RandomGenerator = new RewardChanceRandom(reward);
            g.CombatService.Hit(enemy, enemy.Health);
            Assert(g.InventoryState.Backpack.Count == items + (reward ? 1 : 0), "Elite drop chance is not optional.");
            if (reward) Assert(g.InventoryState.Backpack.Last().Quality >= Rarity.Rare, "Elite dropped common gear.");
            g.CombatService.Hit(enemy, 99);
            Assert(g.PlayerState.Kills == 1 && g.InventoryState.Backpack.Count == items + (reward ? 1 : 0), "Elite rewarded twice.");
        }
        Room(); enemy = new Enemy(p + Vector2I.Right * 3, 'r', 1) { Health = 1 };
        enemy.PromoteElite(EliteTitle.Dread); enemy.Health = 1; dungeon.Enemies.Add(enemy); world.Fire[enemy.Position] = 4; g.RandomGenerator = new RewardChanceRandom(true);
        int beforeItems = g.InventoryState.Backpack.Count; g.EnvironmentService.Tick();
        Assert(g.PlayerState.Kills == 1 && g.InventoryState.Backpack.Count == beforeItems + 1, "Environmental elite kill lost rare reward.");
        GD.Print($"RARE ENCOUNTER AUDIT: {elites}/800 elites, {modifiers}/800 modifiers; four traps, five floor effects, six titles, guardian/refuge safety and rare rewards passed.");
    }
    private sealed class RewardChanceRandom(bool reward) : Random(81)
    {
        public override double NextDouble() => reward ? 0 : .99;
    }
}
