using Godot;
namespace Abyss.Presentation;
internal static class ExitAppearance
{
    internal static bool ShowMarker(DungeonState dungeon, Vector2I position, double time, char foreground)
        => position == dungeon.Stairs && dungeon.Explored[position.X, position.Y]
            && (foreground == '>' || (int)(time * 2) % 2 == 0);
}
