using Godot;
using System;

namespace Abyss.Rules;
internal static class GameRules
{
    internal static int EnemyXp(char glyph, int depth) => glyph == 'B' ? 18 + depth * 2 : EnemyCatalog.Get(glyph, depth).Experience + (depth - 1) / 2;
    internal const int Width = 64;
    internal const int Height = 27;
    internal static bool IsBossFloor(int depth) => depth % 5 == 0;
    internal static int Dist(Vector2I a, Vector2I b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    internal static readonly Vector2I[] Directions =
    {
        Vector2I.Up,
        Vector2I.Down,
        Vector2I.Left,
        Vector2I.Right
    };
    internal static int CycleIndex(int depth) => Math.Max(0, (depth - 1) / 5);
    internal static double ChestChance(int depth) => Math.Min(.60, .42 + .01 * CycleIndex(depth));
    internal static double ExtraChestDropChance(int depth) => Math.Min(.35, .03 * CycleIndex(depth));
}
