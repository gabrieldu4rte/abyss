using Godot;
using System;
using System.Collections.Generic;

namespace Abyss.Domain;
internal sealed class DungeonState
{
    internal EnvironmentState Environment { get; } = new();
    internal bool IsMerchantFloor { get; set; }
    internal Vector2I MerchantPosition { get; set; }
    internal char[, ] Tiles { get; } = new char[GameRules.Width, GameRules.Height];
    internal bool[, ] Explored { get; } = new bool[GameRules.Width, GameRules.Height];
    internal bool[, ] Visible { get; } = new bool[GameRules.Width, GameRules.Height];
    internal List<Enemy> Enemies { get; } = new();
    internal Dictionary<Vector2I, char> Items { get; } = new();
    internal Vector2I Stairs { get; set; }
    internal Rect2I StairsRoom { get; set; }
    internal int Floor { get; set; }

    internal bool Inside(Vector2I p) => p.X > 0 && p.Y > 0 && p.X < GameRules.Width - 1 && p.Y < GameRules.Height - 1;
    internal bool Walk(Vector2I p) => Inside(p) && Tiles[p.X, p.Y] != '#';
    internal Enemy? At(Vector2I p) => Enemies.Find(e => e.Position == p);
    internal bool Los(Vector2I a, Vector2I b)
    {
        int x = a.X, y = a.Y, dx = Math.Abs(b.X - x), dy = -Math.Abs(b.Y - y), sx = x < b.X ? 1 : -1, sy = y < b.Y ? 1 : -1, err = dx + dy;
        while (true)
        {
            if (x == b.X && y == b.Y)
                return true;
            if ((x != a.X || y != a.Y) && Tiles[x, y] == '#')
                return false;
            int e = err * 2;
            if (e >= dy)
            {
                err += dy;
                x += sx;
            }

            if (e <= dx)
            {
                err += dx;
                y += sy;
            }
        }
    }
}
