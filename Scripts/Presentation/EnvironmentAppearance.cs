using Godot;
using System;
using System.Linq;

namespace Abyss.Presentation;
internal static class EnvironmentAppearance
{
    internal static string Name(Biome biome, Localization text) => biome switch
    {
        Biome.Cistern => text.Translate("CISTERNAS ESQUECIDAS", "FORGOTTEN CISTERNS"),
        Biome.FungalCaves => text.Translate("GRUTAS FUNGICAS", "FUNGAL CAVES"),
        Biome.EmberForge => text.Translate("FORJAS DAS BRASAS", "EMBER FORGES"),
        _ => text.Translate("RUINAS ANCESTRAIS", "ANCIENT RUINS")
    };
    internal static (char Glyph, Color Color) Sample(DungeonState dungeon, Vector2I p, double time)
    {
        var world = dungeon.Environment;
        bool visible = dungeon.Visible[p.X, p.Y];
        int frame = visible ? (int)(time * 5 + p.X * .7 + p.Y * .4) : 0;
        char glyph = dungeon.Tiles[p.X, p.Y];
        var wall = new Color(world.Biome switch { Biome.Cistern => "619caf", Biome.FungalCaves => "839575", Biome.EmberForge => "a27b68", _ => "879099" });
        Color color = glyph == '#' ? wall : wall.Darkened(.60f);
        if (glyph == '#')
        {
            bool horizontal = dungeon.Walk(p + Vector2I.Up) || dungeon.Walk(p + Vector2I.Down);
            bool vertical = dungeon.Walk(p + Vector2I.Left) || dungeon.Walk(p + Vector2I.Right);
            glyph = horizontal && vertical ? '+' : horizontal ? '-' : vertical ? '|' : '#';
            if (!horizontal && !vertical) color = wall.Darkened(.8f);
        }
        else if (world.Details.TryGetValue(p, out var detail))
        {
            glyph = detail;
            if (detail == '~') { glyph = "~-=~"[frame % 4]; color = new Color("4a97b2"); }
            else color = new Color(world.Biome == Biome.FungalCaves ? "7d9973" : world.Biome == Biome.EmberForge ? "876c61" : "586770");
        }
        if (world.Oil.Contains(p)) { glyph = 'o'; color = new Color("a29363"); }
        if (world.Fixtures.TryGetValue(p, out var fixture))
        {
            glyph = fixture switch { Fixture.OilBarrel => 'O', Fixture.WallTorch => "YyY*"[frame % 4], Fixture.SpikeTrap => '^', Fixture.ShockTrap => 'Z', Fixture.PoisonTrap => '%', Fixture.FlameTrap => 'V', _ => '_' };
            color = fixture switch { Fixture.OilBarrel => new Color("bd935c"), Fixture.WallTorch => new Color("ffcb6b"), Fixture.SpikeTrap => new Color("c5bec0"), Fixture.ShockTrap => new Color("82c4ef"), Fixture.PoisonTrap => new Color("a3c879"), Fixture.FlameTrap => new Color("ed774b"), _ => new Color("6c775b") };
        }
        if (world.Fire.ContainsKey(p) && visible) { glyph = "*^x+"[frame % 4]; color = new Color(frame % 2 == 0 ? "ffb44f" : "ed774b"); }
        if (dungeon.Tiles[p.X, p.Y] == '>') { glyph = '>'; color = UiTheme.Gold; }
        if (!visible) return (glyph, new Color("26323b"));
        if (dungeon.Modifier != FloorModifier.Blackout && world.Fixtures.Any(f => f.Value == Fixture.WallTorch && GameRules.Dist(p, f.Key) <= 3 && dungeon.Los(p, f.Key)))
            color = color.Lerp(new Color("ffc47a"), .12f + (float)(Math.Sin(time * 6 + p.X) + 1) * .07f);
        return (glyph, color);
    }
}
