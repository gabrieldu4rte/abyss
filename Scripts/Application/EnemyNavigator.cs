using Godot;
using System.Collections.Generic;

namespace Abyss.Application;
internal sealed class EnemyNavigator(DungeonState dungeonState, PlayerState playerState)
{
    internal bool StepEnemy(Enemy enemy, Vector2I destination, bool stayInRoom = false)
    {
        if (enemy.Position == destination)
            return true;
        var queue = new Queue<Vector2I>();
        var previous = new Dictionary<Vector2I, Vector2I>
        {
            {
                enemy.Position,
                enemy.Position
            }
        };
        queue.Enqueue(enemy.Position);
        while (queue.Count > 0 && !previous.ContainsKey(destination))
        {
            var a = queue.Dequeue();
            foreach (var direction in GameRules.Directions)
            {
                var b = a + direction;
                if (!dungeonState.Walk(b) || dungeonState.IsSanctuary(b) || previous.ContainsKey(b) || (stayInRoom && !dungeonState.StairsRoom.HasPoint(b)))
                    continue;
                if (dungeonState.At(b) != null || (b == playerState.Position && b != destination))
                    continue;
                previous[b] = a;
                queue.Enqueue(b);
            }
        }

        if (!previous.ContainsKey(destination))
            return false;
        var step = destination;
        while (previous[step] != enemy.Position)
            step = previous[step];
        if (step == playerState.Position)
            return false;
        enemy.Position = step;
        return true;
    }
}
