using Godot;
using System;

namespace Abyss.Presentation;
internal static class UiTheme
{
    internal static float AsciiGlow(int mode, float x, float y, double time)
    {
        double cx = mode == 3 ? .75 : .5, cy = mode == 2 ? .68 : mode == 3 ? .24 : .45;
        double radius = mode == 1 ? 2 : 9;
        double falloff = Math.Exp(-radius * ((x - cx) * (x - cx) + (y - cy) * (y - cy)));
        double pulse = mode == 1 ? .10 * Math.Sin(time * .85) : .10 * Math.Sin(time * 2.1) + .025 * Math.Sin(time * 5.3);
        return (float)(.88 + falloff * (.16 + pulse));
    }

    internal const int JournalPageSize = 17;
    internal const double HurtDuration = .95;
    internal const float MapX = 272;
    internal const float MapY = 145;
    internal const float CellX = 11;
    internal const float CellY = 16.5f;
    internal static readonly string[] EnglishNames =
    {
        "WARRIOR",
        "MAGE",
        "ARCHER",
        "ROGUE"
    };
    internal static readonly string[] EnglishSkills =
    {
        "Whirlwind",
        "Arcane nova",
        "Piercing arrow",
        "Shadow step"
    };
    internal static bool Previous(Key key) => key == Key.Up || key == Key.W;
    internal static bool Next(Key key) => key == Key.Down || key == Key.S;
    internal static bool Confirm(Key key) => key == Key.Enter || key == Key.Space;
    internal static char ImpactGlyph(double remaining) => ((int)((HurtDuration - remaining) * 15) % 4) switch
    {
        0 => '*',
        1 => '#',
        2 => '!',
        _ => '+'
    };
    internal static string Signed(int value) => value >= 0 ? $"+{value}" : value.ToString();
    internal readonly static string[] Names =
    {
        "GUERREIRO",
        "MAGO",
        "ARQUEIRO",
        "LADINO"
    };
    internal readonly static string[] Skills =
    {
        "Redemoinho",
        "Nova arcana",
        "Flecha precisa",
        "Passo sombrio"
    };
    internal readonly static Color Ink = new("c6d2da");
    internal readonly static Color Dim = new("506174");
    internal readonly static Color Gold = new("e8b86b");
    internal readonly static Color Teal = new("65d9c0");
    internal readonly static Color Red = new("ef7c83");
    internal static Color RarityColor(Rarity r) => r switch
    {
        Rarity.Rare => new Color("69b7ff"),
        Rarity.Epic => new Color("cd8cff"),
        Rarity.Legendary => Gold,
        _ => Ink
    };
    internal static Vector2I MovementDirection(Key key) => key switch
    {
        Key.W or Key.Up => Vector2I.Up,
        Key.S or Key.Down => Vector2I.Down,
        Key.A or Key.Left => Vector2I.Left,
        Key.D or Key.Right => Vector2I.Right,
        _ => Vector2I.Zero
    };
}
