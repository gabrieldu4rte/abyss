using Godot;

namespace Abyss.Infrastructure;
internal sealed class GodotGameHost(Node2D node) : IGameHost
{
    public void RequestRedraw() => node.QueueRedraw();
    public void Quit(int exitCode = 0) => node.GetTree().Quit(exitCode);
    public void Hide() => node.Hide();
    public void ApplyDisplay(DisplayPreferences preferences)
    {
        var window = node.GetWindow();
        var resolution = preferences.Resolution;
        window.ContentScaleMode = Window.ContentScaleModeEnum.Viewport;
        window.ContentScaleAspect = Window.ContentScaleAspectEnum.Keep;
        window.ContentScaleSize = resolution;
        float scale = System.Math.Min(resolution.X / 1280f, resolution.Y / 800f);
        node.Scale = Vector2.One * scale;
        node.Position = ((Vector2)resolution - new Vector2(1280, 800) * scale) / 2;
        window.Mode = preferences.Fullscreen ? Window.ModeEnum.Fullscreen : Window.ModeEnum.Windowed;
        if (!preferences.Fullscreen)
        {
            var usable = DisplayServer.ScreenGetUsableRect(window.CurrentScreen);
            var available = usable.Size - new Vector2I(32, 64);
            window.Size = new Vector2I(System.Math.Min(resolution.X, System.Math.Max(640, available.X)), System.Math.Min(resolution.Y, System.Math.Max(400, available.Y)));
            window.Position = usable.Position + (usable.Size - window.Size) / 2;
        }
        node.QueueRedraw();
    }
}
