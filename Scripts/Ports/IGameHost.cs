namespace Abyss.Ports;
// Engine operations required by application code. No scene-tree dependency in gameplay services.
internal interface IGameHost
{
    void RequestRedraw();
    void Quit(int exitCode = 0);
    void Hide();
    void ToggleFullscreen();
}
