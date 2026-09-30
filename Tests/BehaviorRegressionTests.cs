using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace Abyss.Tests;
internal sealed partial class RegressionSuite
{
    internal void TestBehaviorRegression()
    {
        var hashes = new List<string>();
        void Snapshot(string label)
        {
            var b = new StringBuilder();
            b.Append($"{game.PlayerState.ClassIndex}|{game.DungeonState.Floor}|{game.PlayerState.Health}|{game.PlayerState.MaxHealth}|{game.PlayerState.Energy}|{game.PlayerState.MaxEnergy}|{game.InventoryState.Potions}|{game.InventoryState.EnergyPotions}|{game.PlayerState.Experience}|{game.PlayerState.Level}|{game.PlayerState.Gold}|{game.RunState.Turn}|{game.RunState.Seed}|{game.PlayerState.Kills}|{game.PlayerState.Attributes}|{game.PlayerState.Position}|{game.DungeonState.Stairs}|{game.DungeonState.StairsRoom}|{game.RunState.Screen}|{game.RunState.IsAiming}|{game.DungeonState.IsMerchantFloor}|{game.DungeonState.MerchantPosition}|{game.MenuState.MerchantQuote}|{game.ExpeditionJournal.LastRollPt}|{game.ExpeditionJournal.LastRollEn}|{game.ExpeditionJournal.LastPotionRoll}|{game.ExpeditionJournal.LastPotionHealing}|{game.VisualEffects.HeroHurtRemaining}|{game.VisualEffects.HeroDamage}|{game.VisualEffects.FocusHold}|{game.MenuState.ShopSelling}|{game.MenuState.ShopIndex}|{game.MenuState.ConfirmYes}|{game.MenuState.ExitYes}|{game.MenuState.InventoryNotice}|{game.MenuState.ShopNotice}\n");
            b.Append($"\nLIGHT:{game.InventoryState.SpareTorches}:{game.InventoryState.TorchFuel}:{game.InventoryState.TorchEquipped}");
            var world = game.DungeonState.Environment;
            b.Append($"\nWORLD:{game.DungeonState.Modifier}:{world.Biome}:{world.HeroPoisonTurns}");
            foreach (var entry in world.Details.OrderBy(e => e.Key.Y).ThenBy(e => e.Key.X)) b.Append($"\nD:{entry.Key}:{entry.Value}");
            foreach (var entry in world.Fixtures.OrderBy(e => e.Key.Y).ThenBy(e => e.Key.X)) b.Append($"\nX:{entry.Key}:{entry.Value}");
            foreach (var p in world.Oil.OrderBy(p => p.Y).ThenBy(p => p.X)) b.Append($"\nOIL:{p}");
            foreach (var entry in world.Fire.OrderBy(e => e.Key.Y).ThenBy(e => e.Key.X)) b.Append($"\nFIRE:{entry.Key}:{entry.Value}");
            foreach (var entry in world.PoisonedEnemies.OrderBy(e => e.Key.Position.Y).ThenBy(e => e.Key.Position.X)) b.Append($"\nPOISON:{entry.Key.Position}:{entry.Value}");
            for (int y = 0; y < GameRules.Height; y++)
                for (int x = 0; x < GameRules.Width; x++)
                    b.Append($"{game.DungeonState.Tiles[x, y]}{game.DungeonState.Explored[x, y]}{game.DungeonState.Visible[x, y]}");
            foreach (Enemy e in game.DungeonState.Enemies)
                b.Append($"\nE:{e.Position}|{e.Health}|{e.MaxHealth}|{e.Depth}|{e.Glyph}|{string.Join(",", e.Titles)}|{e.Stats}|{e.Alerted}|{e.SearchTurns}|{e.PatrolTarget}|{e.LastSeen}|{e.Hurt}|{e.LastDamage}");
            foreach (var item in game.DungeonState.Items.OrderBy(p => p.Key.Y).ThenBy(p => p.Key.X))
                b.Append($"\nI:{item.Key}:{item.Value}");
            string GearText(Gear? g) => g == null ? "none" : $"{g.Kind}:{g.Quality}:{g.Grade}";
            foreach (var g in game.InventoryState.Backpack)
                b.Append("\nG:" + GearText(g));
            foreach (var g in game.InventoryState.Equipped)
                b.Append("\nS:" + GearText(g) + ":" + game.InventoryState.Backpack.FindIndex(item => ReferenceEquals(item, g)));
            foreach (Offer o in game.MerchantState.MerchantStock)
                b.Append($"\nO:{GearText(o.Gear)}:{o.Potion}:{o.Quantity}");
            if (game.MenuState.PendingTrade != null)
                b.Append($"\nT:{GearText(game.MenuState.PendingTrade.Gear)}:{game.MenuState.PendingTrade.Potion}:{game.MenuState.PendingTrade.Quantity}");
            foreach (var entry in game.ExpeditionJournal.Entries)
                b.Append($"\nL:{entry.Pt}|{entry.En}");
            foreach (DamageEffect effect in game.VisualEffects.Effects)
                b.Append($"\nF:{effect.Position}:{effect.Damage}:{effect.Hero}:{effect.Remaining}");
            b.Append($"\nFOCUS:{game.VisualEffects.Focus?.Position}:{game.VisualEffects.Focus?.Health}:{game.VisualEffects.Focus?.Glyph}");
            hashes.Add(label + " " + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(b.ToString()))));
        }

        game.MenuState.IsTesting = true;
        game.MenuState.English = false;
        for (int hero = 0; hero < 4; hero++)
            foreach (int depth in new[]
            {
                1,
                2,
                5,
                6,
                10,
                25,
                100,
                1000
            }

            )
            {
                game.PlayerState.ClassIndex = hero;
                game.Start(900 + hero);
                game.DungeonState.Floor = depth;
                game.DungeonGenerator.Generate(false);
                Snapshot($"map-{hero}-{depth}");
                var actions = new Random(713 + hero + depth);
                for (int i = 0; i < 32; i++)
                {
                    if (game.RunState.Screen == "dead")
                        break;
                    game.GameInput.HandleKey(new[] { Key.W, Key.A, Key.S, Key.D, Key.Space, Key.P, Key.Q, Key.F, Key.Tab }[actions.Next(9)]);
                    game.VisualEffects.AdvanceEffects(.17);
                    Snapshot($"action-{hero}-{depth}-{i}");
                }

                game.RunState.Screen = "game";
                game.RunState.IsAiming = false;
                game.DungeonState.Enemies.Clear();
                game.PlayerState.Health = game.PlayerState.MaxHealth;
                game.PlayerState.Energy = game.PlayerState.MaxEnergy;
                foreach (char glyph in new[]
                {
                    'r',
                    's',
                    'g',
                    'B'
                }

                )
                {
                    var enemy = new Enemy(game.PlayerState.Position + Vector2I.Right, glyph, depth);
                    game.DungeonState.Enemies.Add(enemy);
                    game.CombatService.Hit(enemy, enemy.Health);
                    Snapshot($"reward-{hero}-{depth}-{glyph}");
                }

                game.LootService.OpenChest();
                Snapshot($"chest-{hero}-{depth}");
                game.DungeonState.Floor = depth + 1;
                game.DungeonGenerator.Generate(true);
                game.PlayerState.Gold = 5000;
                game.PlayerState.Position = game.DungeonState.MerchantPosition + Vector2I.Down;
                game.PlayerActions.Interact();
                Snapshot($"merchant-{hero}-{depth}");
                game.MenuController.HandleShop(Key.Enter);
                game.MenuController.HandleShop(Key.Down);
                game.MenuController.HandleShop(Key.Enter);
                Snapshot($"buy-{hero}-{depth}");
                game.MenuController.HandleShop(Key.Right);
                game.MenuController.HandleShop(Key.Enter);
                game.MenuController.HandleShop(Key.Down);
                game.MenuController.HandleShop(Key.Enter);
                Snapshot($"sell-{hero}-{depth}");
            }

        string text = string.Join("\n", hashes) + "\n";
        var record = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--record-regression="));
        if (record != null)
            System.IO.File.WriteAllText(record.Substring(20), text);
        else
        {
            string expected = Godot.FileAccess.GetFileAsString("res://Tests/Fixtures/environment-behavior.sha256");
            var actualLines = text.Split('\n');
            var expectedLines = expected.Split('\n');
            if (!actualLines.SequenceEqual(expectedLines))
            {
                int first = Enumerable.Range(0, Math.Min(actualLines.Length, expectedLines.Length)).FirstOrDefault(i => actualLines[i] != expectedLines[i]);
                throw new Exception($"Behavior changed at checkpoint {first}: {actualLines[first]}");
            }
        }

        game.MenuState.IsTesting = false;
        GD.Print($"BEHAVIOR AUDIT: {hashes.Count} deterministic checkpoints match the environmental gameplay baseline.");
    }
}
