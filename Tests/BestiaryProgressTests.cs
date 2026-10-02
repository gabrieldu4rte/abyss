using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
namespace Abyss.Tests;
internal sealed partial class RegressionSuite
{
    internal void TestBestiaryProgress()
    {
        void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        var store = new MemoryBestiaryStore();
        var canvas = new BestiaryRecordingCanvas();
        var g = new GameSession(new TestHost(), canvas, new TestSettings(), store);
        g.AsciiCanvas.Font = GD.Load<Font>("res://Mono.ttf");
        g.Start(51); g.DungeonState.Enemies.Clear(); g.DungeonState.Environment.Clear();
        Check(g.BestiaryState.Defeated.Count == 0, "New bestiary is not locked.");
        for (int biome = 0; biome < 4; biome++)
        {
            int depth = biome * 5 + 1;
            g.MenuState.BestiaryBiome = biome;
            foreach (char glyph in EnemyCatalog.Roster(depth) + "B")
            {
                var enemy = new Enemy(g.PlayerState.Position, glyph, depth);
                string species = enemy.Profile.ArtKey;
                foreach (bool english in new[] { false, true })
                {
                    g.MenuState.English = english;
                    canvas.Text.Clear(); g.Transitions.BeginFrame("bestiary"); g.BestiaryRenderer.Draw(); g.Transitions.EndFrame();
                    Check(!canvas.Text.Contains(EnemyText.Name(glyph, depth, english).ToUpperInvariant()), "Locked bestiary exposes a creature name.");
                    string lore = BestiaryLore.Description(species, english);
                    Check(lore.Length > 80 && !lore.Any(char.IsDigit), "Bestiary lacks lore or exposes numbers.");
                }
                g.DungeonState.Enemies.Add(enemy);
                g.CombatService.Hit(enemy, 1);
                Check(g.BestiaryState.Count(species) == 0, "Nonlethal hit unlocked bestiary.");
                g.CombatService.Hit(enemy, enemy.Health);
                g.CombatService.Hit(enemy, 999);
                Check(g.BestiaryState.Count(species) == 1, "Defeat not counted exactly once.");
                canvas.Text.Clear(); g.Transitions.BeginFrame("bestiary"); g.BestiaryRenderer.Draw(); g.Transitions.EndFrame();
                Check(canvas.Text.Contains(EnemyText.Name(glyph, depth, true).ToUpperInvariant()), "Unlocked entry hides its name.");
                g.MenuState.BestiaryEntry = (g.MenuState.BestiaryEntry + 1) % 5;
            }
        }
        var elite = new Enemy(g.PlayerState.Position, 's', 21); elite.PromoteElite(EliteTitle.Cruel);
        g.DungeonState.Enemies.Add(elite); g.CombatService.Hit(elite, elite.Health);
        Check(g.BestiaryState.Count("skeleton") == 2 && g.BestiaryState.Defeated.Count == 20, "Elite/cycle count split the species.");
        g.Start(52);
        Check(g.BestiaryState.Count("skeleton") == 2, "Starting another expedition cleared bestiary.");
        var loaded = new GameSession(new TestHost(), new TestCanvas(), new TestSettings(), store);
        Check(loaded.BestiaryState.Count("skeleton") == 2 && loaded.BestiaryState.Defeated.Count == 20, "Discoveries did not survive reopening.");
        loaded.Start(54); loaded.DungeonState.Enemies.Clear(); loaded.DungeonState.Environment.Clear();
        var rat = new Enemy(loaded.PlayerState.Position + Vector2I.Right, 'r', 6) { Health = 1 };
        loaded.DungeonState.Enemies.Add(rat); loaded.DungeonState.Environment.Fire[rat.Position] = 2;
        loaded.EnvironmentService.Tick(); loaded.EnvironmentService.Tick();
        Check(loaded.BestiaryState.Count("rat") == 2, "Environmental kill not counted exactly once.");
        g.RunState.Screen = "pause"; g.MenuState.PauseTab = 3; g.MenuState.HelpTopic = 2;
        int turn = g.RunState.Turn;
        g.MenuController.HandlePause(Key.Enter);
        Check(g.MenuState.BestiaryOpen, "Bestiary cannot be opened from compendium.");
        g.MenuController.HandlePause(Key.Left);
        Check(g.MenuState.BestiaryBiome == 3 && g.MenuState.PauseTab == 3, "Biome navigation changed tabs.");
        g.MenuController.HandlePause(Key.Up);
        Check(g.MenuState.BestiaryEntry == 4, "Creature navigation does not wrap.");
        g.MenuController.HandlePause(Key.Escape);
        Check(!g.MenuState.BestiaryOpen && g.RunState.Screen == "pause", "Escape should return to compendium.");
        g.MenuController.HandlePause(Key.Escape);
        Check(g.RunState.Screen == "game" && g.RunState.Turn == turn, "Bestiary navigation consumed turns.");
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "abyss-bestiary-" + Guid.NewGuid() + ".cfg");
        try
        {
            var disk = new GodotBestiaryStore(path);
            Check(disk.Load().Count == 0, "Missing bestiary file should be empty.");
            Check(disk.Save(new Dictionary<string, int> { ["skeleton"] = 3 }), "Bestiary save failed.");
            Check(disk.Save(new Dictionary<string, int> { ["skeleton"] = 4, ["tide_warden"] = 1 }), "Bestiary replacement failed.");
            var reopened = new GodotBestiaryStore(path).Load();
            Check(reopened["skeleton"] == 4 && reopened["tide_warden"] == 1, "Disk bestiary lost counts.");
        }
        finally { System.IO.File.Delete(path); System.IO.File.Delete(path + ".tmp"); }
        GD.Print("BESTIARY PROGRESS AUDIT: 20 locked/unlocked bilingual entries, exact kills, elites, environmental kills, expedition/restart persistence, disk replacement and turn-free navigation passed.");
    }
    private sealed class MemoryBestiaryStore : IBestiaryStore
    {
        private Dictionary<string, int> entries = new();
        public IReadOnlyDictionary<string, int> Load() => entries;
        public bool Save(IReadOnlyDictionary<string, int> defeated) { entries = new(defeated); return true; }
    }
    private sealed class BestiaryRecordingCanvas : IAsciiCanvas
    {
        internal List<string> Text { get; } = new();
        public void DrawString(Font font, Vector2 position, string text, HorizontalAlignment alignment, float width, int fontSize, Color color) => Text.Add(text);
        public void DrawSetTransform(Vector2 position, float rotation, Vector2 scale) { }
    }
}
