using Godot;

namespace Abyss.Infrastructure;
internal sealed class GodotAsciiCanvas(Node2D node) : IAsciiCanvas
{
    public void DrawString(Font font, Vector2 position, string text, HorizontalAlignment alignment, float width, int fontSize, Color color) => node.DrawString(font, position, text, alignment, width, fontSize, color);
    public void DrawSetTransform(Vector2 position, float rotation, Vector2 scale) => node.DrawSetTransform(position, rotation, scale);
}
