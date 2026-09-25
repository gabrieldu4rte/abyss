using Godot;
using static Abyss.Rules.TabletopRules;

namespace Abyss.Domain;
internal sealed class PlayerState
{
    internal Attributes Attributes { get; set; }
    internal Vector2I Position { get; set; }
    internal int ClassIndex { get; set; }
    internal int Health { get; set; }
    internal int MaxHealth { get; set; }
    internal int Energy { get; set; }
    internal int MaxEnergy { get; set; }
    internal int Experience { get; set; }
    internal int Level { get; set; }
    internal int Gold { get; set; }
    internal int Kills { get; set; }
}
