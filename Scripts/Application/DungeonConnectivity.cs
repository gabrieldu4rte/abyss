using Godot;
using System.Collections.Generic;
namespace Abyss.Application;
internal static class DungeonConnectivity
{
    internal static void EnsureExit(DungeonState dungeon, Vector2I entrance)
    {
        dungeon.Tiles[dungeon.Stairs.X, dungeon.Stairs.Y] = '>';
        var reached = new HashSet<Vector2I>();
        void Flood(Vector2I origin)
        {
            var pending = new Queue<Vector2I>();
            if (reached.Add(origin)) pending.Enqueue(origin);
            while (pending.Count > 0)
            {
                var current = pending.Dequeue();
                foreach (var direction in GameRules.Directions)
                {
                    var next = current + direction;
                    if (dungeon.Walk(next) && reached.Add(next)) pending.Enqueue(next);
                }
            }
        }
        Flood(entrance);
        for (int y = 1; y < GameRules.Height - 1; y++)
            for (int x = 1; x < GameRules.Width - 1; x++)
            {
                var position = new Vector2I(x, y);
                if (!dungeon.Walk(position) || reached.Contains(position)) continue;
                var step = position;
                while (step != entrance)
                {
                    if (step != dungeon.Stairs) dungeon.Tiles[step.X, step.Y] = '.';
                    if (step.X != entrance.X) step.X += System.Math.Sign(entrance.X - step.X);
                    else step.Y += System.Math.Sign(entrance.Y - step.Y);
                }
                reached.Clear();
                Flood(entrance);
            }
    }
}
