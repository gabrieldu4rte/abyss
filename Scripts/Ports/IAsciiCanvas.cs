using Godot;

namespace Abyss.Ports;
internal interface IAsciiCanvas
{
    void DrawString(Font font, Vector2 position, string text, HorizontalAlignment alignment, float width, int fontSize, Color color);
    void DrawSetTransform(Vector2 position, float rotation, Vector2 scale);
}
