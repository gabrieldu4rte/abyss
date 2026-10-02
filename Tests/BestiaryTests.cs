using Godot;
using System;
using System.Linq;
namespace Abyss.Tests;
internal sealed partial class RegressionSuite
{
    internal void TestBestiary()
    {
        void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        var portraits = new System.Collections.Generic.HashSet<string>();
        for (int biome = 0; biome < 4; biome++)
        {
            int depth = biome * 5 + 5;
            var roster = EnemyCatalog.Roster(depth);
            Check(roster.Length == 4 && roster.Distinct().Count() == 4, "Biome lacks four species.");
            foreach (char glyph in roster.Append('B'))
            {
                var enemy = new Enemy(Vector2I.Zero, glyph, depth);
                Check(portraits.Add(AsciiArt.Enemy(enemy)), "Bestiary portrait is reused across species.");
                Check(EnemyText.Name(glyph, depth, true).Length > 2 && EnemyText.Name(glyph, depth, false).Length > 2, "Missing bestiary localization.");
            }
            var spawned = new System.Collections.Generic.HashSet<char>();
            for (int seed = 1; seed <= 30; seed++)
            {
                game.Start(seed);
                game.DungeonState.Floor = depth;
                game.DungeonGenerator.Generate(false);
                foreach (var spawnedEnemy in game.DungeonState.Enemies) spawned.Add(spawnedEnemy.Glyph);
                Check(game.DungeonState.Enemies.All(e => e.IsWarden || roster.Contains(e.Glyph)), "Foreign biome enemy spawned.");
                Check(game.DungeonState.Enemies.Single(e => e.IsWarden).HomeBiome == (Biome)biome, "Wrong biome Warden.");
            }
            Check(roster.All(spawned.Contains), "Biome roster includes a species that never spawns.");
            Enemy Prepare()
            {
                game.Start(712);
                var world = game.DungeonState;
                world.Floor = depth;
                world.Enemies.Clear(); world.Items.Clear(); world.Environment.Clear(); world.Modifier = FloorModifier.None;
                world.StairsRoom = new Rect2I(20, 5, 15, 15); world.Stairs = new Vector2I(21, 6);
                for (int y = 1; y < GameRules.Height - 1; y++)
                    for (int x = 1; x < GameRules.Width - 1; x++) world.Tiles[x, y] = '.';
                game.PlayerState.Position = new Vector2I(28, 12);
                game.PlayerState.Health = game.PlayerState.MaxHealth = 1000;
                var boss = new Enemy(new Vector2I(26, 12), 'B', depth) { Alerted = true, AbilityCooldown = 0 };
                world.Enemies.Add(boss); game.DungeonGenerator.Reveal(); game.RandomGenerator = new FixedRandom(20);
                return boss;
            }
            var boss = Prepare();
            var abilities = new WardenAbilities(game.CombatService, game.DungeonState, game.PlayerState, game.ExpeditionJournal, game.VisualEffects);
            Check(abilities.Act(boss, false) && boss.AbilityWindup == 2 && boss.AbilityEnergy == 2, "Warden did not prepare or pay energy.");
            Check(boss.AbilityCells.All(p => game.DungeonState.StairsRoom.HasPoint(p) && game.DungeonState.Walk(p)), "Warden mask escaped room.");
            int hp = game.PlayerState.Health;
            Check(abilities.Act(boss, false) && game.PlayerState.Health == hp && boss.AbilityWindup == 1, "Windup damaged too early.");
            abilities.Act(boss, false);
            Check(game.PlayerState.Health < hp && boss.AbilityCells.Count == 0 && boss.AbilityCooldown == 4, "Warden ability did not resolve.");
            Check(game.VisualEffects.Actions.Animations.Single().Kind == (ActionAnimationKind)((int)ActionAnimationKind.SeismicImpact + biome), "Wrong Warden animation.");
            if (biome == 1) Check(game.DungeonState.Environment.Details.ContainsKey(game.PlayerState.Position), "Flood did not leave water.");
            if (biome == 2) Check(game.DungeonState.Environment.HeroPoisonTurns == 3, "Spore poison missing.");
            if (biome == 3) Check(game.DungeonState.Environment.Fire.Count > 0, "Furnace fire missing.");
            var animation = game.VisualEffects.Actions.Animations.Single();
            animation.Advance(.3); var early = animation.Sample().ToArray(); animation.Advance(.2);
            Check(early.Length > 0 && !early.SequenceEqual(animation.Sample()) && animation.Sample().All(g => g.Character is >= ' ' and <= '~'), "Warden effect did not animate in ASCII.");
            hp = game.PlayerState.Health; int turn = game.RunState.Turn;
            game.VisualEffects.AdvanceEffects(2);
            Check(hp == game.PlayerState.Health && turn == game.RunState.Turn, "Cosmetic animation advanced combat.");
            boss = Prepare(); abilities.Act(boss, false);
            game.PlayerState.Position = new Vector2I(22, 8); hp = game.PlayerState.Health;
            abilities.Act(boss, false); abilities.Act(boss, false);
            Check(hp == game.PlayerState.Health, "Escaping a marked area did not evade the ability.");
            boss = Prepare(); abilities.Act(boss, false);
            game.PlayerState.Position = new Vector2I(19, 8);
            Check(!abilities.Act(boss, false) && boss.AbilityCells.Count == 0 && boss.AbilityWindup == 0, "Leaving room did not cancel Warden ability.");
        }
        GD.Print("BESTIARY AUDIT: 16 biome species, four unique Wardens, 120 floors, localized portraits, energy, windup, dodging, confinement, four animated skills and environmental effects passed.");
    }
}
