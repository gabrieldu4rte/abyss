using Godot;
using System;
using System.Linq;

namespace Abyss.Application;
internal sealed class FloorEventGenerator(DungeonState dungeon, PlayerState player, RunState run, ExpeditionJournal journal)
{
    internal const double ModifierChance = .12;
    internal const double EliteChance = .08;
    internal void Generate(FloorModifier? forcedModifier = null, bool? forceElite = null)
    {
        dungeon.Modifier = FloorModifier.None;
        if (dungeon.IsMerchantFloor) return;
        var random = new Random(unchecked(run.Seed * 104729 ^ dungeon.Floor * 65537 ^ 1741));
        var options = Enum.GetValues<FloorModifier>().Where(m => m != FloorModifier.None && !(m == FloorModifier.Infestation && GameRules.IsBossFloor(dungeon.Floor))).ToArray();
        var modifier = forcedModifier ?? (random.NextDouble() < ModifierChance ? options[random.Next(options.Length)] : FloorModifier.None);
        dungeon.Modifier = modifier;
        if (modifier == FloorModifier.Infestation)
        {
            char glyph = "rsg"[random.Next(3)];
            for (int i = 0; i < dungeon.Enemies.Count; i++)
                if (dungeon.Enemies[i].Glyph != 'B') dungeon.Enemies[i] = new Enemy(dungeon.Enemies[i].Position, glyph, dungeon.Floor);
        }
        if (modifier == FloorModifier.HiddenCache)
        {
            var cells = new System.Collections.Generic.List<Vector2I>();
            for (int y = 1; y < GameRules.Height - 1; y++)
                for (int x = 1; x < GameRules.Width - 1; x++)
                {
                    var p = new Vector2I(x, y);
                    if (dungeon.Walk(p) && p != dungeon.Stairs && GameRules.Dist(p, player.Position) > 5 && dungeon.At(p) == null && !dungeon.Items.ContainsKey(p) && !dungeon.Environment.Fixtures.ContainsKey(p)) cells.Add(p);
                }
            for (int i = 0; i < 2 && cells.Count > 0; i++)
            {
                int index = random.Next(cells.Count);
                dungeon.Items[cells[index]] = 'C';
                dungeon.Environment.Details.Remove(cells[index]);
                cells.RemoveAt(index);
            }
        }
        if ((forceElite ?? random.NextDouble() < EliteChance) && !dungeon.Enemies.Any(e => e.IsElite))
        {
            var candidates = dungeon.Enemies.Where(e => e.Glyph != 'B').ToArray();
            if (candidates.Length > 0)
            {
                var enemy = candidates[random.Next(candidates.Length)];
                var titles = Enum.GetValues<EliteTitle>().OrderBy(_ => random.Next()).Take(random.NextDouble() < .4 ? 2 : 1).ToArray();
                enemy.PromoteElite(titles);
            }
        }
        if (modifier != FloorModifier.None)
            journal.Say(FloorEventText.Description(modifier, false), FloorEventText.Description(modifier, true));
    }
}
