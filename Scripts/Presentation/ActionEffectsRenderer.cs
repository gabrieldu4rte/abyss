using Godot;
using System.Collections.Generic;

namespace Abyss.Presentation;

internal sealed class ActionEffectsRenderer(AsciiCanvas canvas, DungeonState dungeon, ActionEffects effects)
{
    internal IReadOnlyDictionary<Vector2I, ActionGlyph> GetFrame()
    {
        var frame = new Dictionary<Vector2I, ActionGlyph>();
        foreach (var enemy in dungeon.Enemies)
            if (enemy.IsWarden && enemy.Health > 0 && enemy.AbilityWindup > 0)
                foreach (var p in enemy.AbilityCells)
                    if (dungeon.Inside(p) && dungeon.Visible[p.X, p.Y] && dungeon.Walk(p))
                        frame[p] = new(p, '!', new Color(enemy.HomeBiome switch {
                            Biome.Cistern => "70cfff", Biome.FungalCaves => "b5e87a",
                            Biome.EmberForge => "ff8844", _ => "ffd58a" }));
        foreach (var animation in effects.Animations)
            foreach (var glyph in animation.Sample())
            {
                var p = glyph.Position;
                if (!animation.VisibleCells.Contains(p) || !dungeon.Inside(p) || !dungeon.Visible[p.X, p.Y] || !dungeon.Walk(p)) continue;
                frame[p] = glyph;
            }
        return frame;
    }

    internal void Draw()
    {
        foreach (var glyph in GetFrame().Values)
            canvas.Text(UiTheme.MapX + glyph.Position.X * UiTheme.CellX, UiTheme.MapY + glyph.Position.Y * UiTheme.CellY,
                glyph.Character.ToString(), glyph.Color, 17);
    }
}
