using Godot;
using System;
using System.Collections.Generic;

namespace Abyss.Presentation;
internal sealed class ScreenTransitions(IAsciiCanvas target) : IAsciiCanvas
{
    private List<Action<float>> current = new();
    private List<Action<float>> previous = new();
    private string route = "";
    private double elapsed = 1;
    private double halfDuration = .18;
    private Func<bool>? waitForSound;
    internal bool Enabled { get; set; }
    internal bool Active => Enabled && elapsed < halfDuration * 2;
    internal void BeginFloorChange(Func<bool>? soundPlaying = null)
    {
        if (!Enabled) return;
        previous = current;
        waitForSound = soundPlaying;
        halfDuration = .4;
        elapsed = 0;
    }
    internal void Advance(double delta)
    {
        elapsed += delta;
        if (waitForSound?.Invoke() == true && elapsed >= halfDuration)
            elapsed = halfDuration;
        else if (elapsed >= halfDuration)
            waitForSound = null;
    }
    internal void BeginFrame(string nextRoute)
    {
        if (route != nextRoute)
        {
            previous = current;
            waitForSound = null;
            halfDuration = .18;
            elapsed = Enabled && route.Length > 0 ? 0 : 1;
            route = nextRoute;
        }
        current = new();
    }
    internal void EndFrame()
    {
        bool outgoing = Active && elapsed < halfDuration;
        float progress = Active ? (float)(outgoing ? 1 - elapsed / halfDuration : (elapsed - halfDuration) / halfDuration) : 1;
        float opacity = progress * progress * (3 - 2 * progress);
        target.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        foreach (var command in outgoing ? previous : current)
            command(opacity);
        target.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        if (!Active) previous.Clear();
    }
    public void DrawString(Font font, Vector2 position, string text, HorizontalAlignment alignment, float width, int fontSize, Color color)
    {
        current.Add(opacity => target.DrawString(font, position, text, alignment, width, fontSize, new Color(color.R, color.G, color.B, color.A * opacity)));
    }
    public void DrawSetTransform(Vector2 position, float rotation, Vector2 scale)
        => current.Add(_ => target.DrawSetTransform(position, rotation, scale));
}
