namespace Abyss.Ports;
internal interface IGameHost
{
    void RequestRedraw();
    void Quit(int exitCode = 0);
    void Hide();
    void ApplyDisplay(DisplayPreferences preferences);
}
