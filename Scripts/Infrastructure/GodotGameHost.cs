using Godot;

namespace Abyss.Infrastructure;
internal sealed class GodotGameHost(Node2D node) : IGameHost
{
    public void RequestRedraw() => node.QueueRedraw();
    public void Quit(int exitCode = 0) => node.GetTree().Quit(exitCode);
    public void Hide() => node.Hide();
    public void ToggleFullscreen() => DisplayServer.WindowSetMode(DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen ? DisplayServer.WindowMode.Windowed : DisplayServer.WindowMode.Fullscreen);
}
