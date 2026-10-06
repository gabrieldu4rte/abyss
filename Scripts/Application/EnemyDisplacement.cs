using Godot;
using System;
namespace Abyss.Application;
internal static class EnemyDisplacement
{
    internal static void Push(DungeonState dungeon, PlayerState player, Enemy enemy, Vector2I origin)
    {
        var delta = enemy.Position - origin;
        var direction = Math.Abs(delta.X) >= Math.Abs(delta.Y) ? new Vector2I(Math.Sign(delta.X),0) : new Vector2I(0,Math.Sign(delta.Y));
        var next = enemy.Position + direction;
        if (direction != Vector2I.Zero && dungeon.Walk(next) && !dungeon.IsSanctuary(next) && next != player.Position && dungeon.At(next) == null && (!enemy.IsWarden || enemy.Alerted || dungeon.StairsRoom.HasPoint(next))) enemy.Position = next;
    }
}
