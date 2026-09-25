using Godot;

namespace Abyss.Presentation;
internal sealed class DamageEffect
{
    public Vector2I Position { get; set; }
    public int Damage { get; set; }
    public bool Hero { get; set; }
    public double Remaining { get; set; } = UiTheme.HurtDuration;
}
