using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Abyss.Tests;
internal sealed partial class RegressionSuite
{
    private readonly GameSession game;
    internal RegressionSuite(GameSession game)
    {
        this.game = game;
    }

    internal sealed class FixedRandom : Random
    {
        readonly int value;
        public FixedRandom(int value)
        {
            this.value = value;
        }

        public override int Next(int minValue, int maxValue) => Math.Clamp(value, minValue, maxValue - 1);
        public override double NextDouble() => .5;
    }

    internal sealed class RangeRandom : Random
    {
        readonly int value;
        public RangeRandom(int value)
        {
            this.value = value;
        }

        public override int Next(int maxValue) => Math.Clamp(value, 0, maxValue - 1);
    }

    internal void SelfTest()
    {
        game.Host.Hide();
        try
        {
            for (int s = 1; s <= 50; s++)
                foreach (int f in new[]
                {
                    1,
                    4,
                    5,
                    6,
                    10,
                    15,
                    25,
                    50,
                    100,
                    500,
                    1000
                }

                )
                {
                    game.PlayerState.ClassIndex = s % 4;
                    game.Start(s);
                    game.DungeonState.Floor = f;
                    game.DungeonGenerator.Generate();
                    var reached = new HashSet<Vector2I>
                    {
                        game.PlayerState.Position
                    };
                    var q = new Queue<Vector2I>();
                    q.Enqueue(game.PlayerState.Position);
                    while (q.Count > 0)
                    {
                        var a = q.Dequeue();
                        foreach (var d in GameRules.Directions)
                        {
                            var b = a + d;
                            if (game.DungeonState.Walk(b) && reached.Add(b))
                                q.Enqueue(b);
                        }
                    }

                    if (!reached.Contains(game.DungeonState.Stairs) || game.DungeonState.Enemies.Any(e => !reached.Contains(e.Position)) || game.DungeonState.Items.Keys.Any(p => !reached.Contains(p)))
                        throw new Exception("Unreachable content");
                    if (game.DungeonState.Enemies.Select(e => e.Position).Distinct().Count() != game.DungeonState.Enemies.Count || game.DungeonState.Enemies.Any(e => game.DungeonState.Items.ContainsKey(e.Position) || e.Position == game.PlayerState.Position))
                        throw new Exception("Overlapping entities");
                    for (int x = 0; x < GameRules.Width; x++)
                        if (game.DungeonState.Tiles[x, 0] != '#' || game.DungeonState.Tiles[x, GameRules.Height - 1] != '#')
                            throw new Exception("Open border");
                    if (game.DungeonState.Enemies.Count(e => e.Glyph == 'B') != (GameRules.IsBossFloor(f) ? 1 : 0) || game.DungeonState.Enemies.Count > 15)
                        throw new Exception("Boss cadence or density failed");
                    game.RandomGenerator = new Random(s);
                    game.DungeonState.Floor = f;
                    game.DungeonGenerator.Generate();
                    var first = new string (game.DungeonState.Tiles.Cast<char>().ToArray());
                    game.RandomGenerator = new Random(s);
                    game.DungeonGenerator.Generate();
                    if (first != new string (game.DungeonState.Tiles.Cast<char>().ToArray()))
                        throw new Exception("Non deterministic");
                }

            for (game.PlayerState.ClassIndex = 0; game.PlayerState.ClassIndex < 4; game.PlayerState.ClassIndex++)
            {
                game.Start(777);
                game.DungeonState.Enemies.Clear();
                var p = GameRules.Directions.Select(d => game.PlayerState.Position + d).First(game.DungeonState.Walk);
                var e = new Enemy(p, 'r', 1)
                {
                    Health = 1,
                    MaxHealth = 1
                };
                game.DungeonState.Enemies.Add(e);
                game.RandomGenerator = new RegressionSuite.FixedRandom(20);
                if (game.PlayerState.ClassIndex is 1 or 2)
                    game.PlayerActions.Shoot(p - game.PlayerState.Position);
                else
                    game.PlayerActions.Move(p - game.PlayerState.Position);
                if (game.DungeonState.Enemies.Count != 0 || game.PlayerState.Experience != GameRules.EnemyXp('r', 1))
                    throw new Exception("Melee failed");
                game.PlayerState.Health = 1;
                game.InventoryState.Potions = 2;
                game.PlayerActions.Drink();
                if (game.PlayerState.Health != 21 || game.InventoryState.Potions != 1)
                    throw new Exception("Potion failed");
                game.PlayerState.Position = game.DungeonState.Stairs;
                game.Descend();
                if (game.DungeonState.Floor != 2)
                    throw new Exception("Descent failed");
                foreach (int bossFloor in new[]
                {
                    5,
                    10,
                    25,
                    100
                }

                )
                {
                    game.DungeonState.Floor = bossFloor;
                    game.DungeonGenerator.Generate();
                    game.PlayerState.Position = game.DungeonState.Stairs;
                    int previousHp = game.PlayerState.Health, previousEnergy = game.PlayerState.Energy;
                    game.Descend();
                    if (game.DungeonState.Floor != bossFloor || game.PlayerState.Health != previousHp || game.PlayerState.Energy != previousEnergy)
                        throw new Exception("Living boss did not block descent");
                    var boss = game.DungeonState.Enemies.Single(e => e.Glyph == 'B');
                    int previousPotions = game.InventoryState.Potions;
                    game.CombatService.Hit(boss, boss.Health);
                    if (game.InventoryState.Potions != previousPotions + 1)
                        throw new Exception("Boss potion reward missing");
                    game.Descend();
                    if (game.DungeonState.Floor != bossFloor + 1 || game.RunState.Screen != "game")
                        throw new Exception("Endless descent failed");
                }
            }

            TestPresentation();
            TestSettingsMenu();
            TestAdvancement();
            TestElements();
            TestBalance();
            TestEnemyAi();
            TestInventory();
            TestMerchant();
            TestBlacksmith();
            TestCycleBalance();
            TestHeldMovement();
            TestEnvironment();
            TestExits();
            TestRareEncounters();
            TestBestiary();
            TestBestiaryProgress();
            TestNamedItems();
            TestAdditionalNamedItems();
            TestBehaviorRegression();
            TestArchitecture();
            TestActionAnimations();
            TestScreenTransitions();
            TestAudio();
            GD.Print("SELF-TEST PASS: d20, criticals, 2d10 healing, attributes, scarce loot, scaling, costs, ranged restrictions, monster names, localization, menus, portraits, damage effects; 550 generated floors through depth 1000, reachability, occupancy, deterministic seeds, all classes, combat, healing, endless descent, boss gates and rewards; patrol, sight, pursuit, search, boss-room confinement.");
            game.Host.Quit();
        }
        catch (Exception e)
        {
            GD.PushError(e.ToString());
            game.Host.Quit(1);
        }
    }
}
