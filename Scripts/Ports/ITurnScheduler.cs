using Godot;

namespace Abyss.Ports;
internal interface ITurnScheduler
{
    void EndTurn(bool evade = false, Vector2I? previousPlayer = null);
}
