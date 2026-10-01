using Godot;
using System.Collections.Generic;

namespace Abyss.Domain;
internal enum Biome { Ruins, Cistern, FungalCaves, EmberForge }
internal enum Fixture { OilBarrel, WallTorch, PoisonTrap, SpentTrap, SpikeTrap, ShockTrap, FlameTrap }
internal sealed class EnvironmentState
{
    internal Biome Biome { get; set; }
    internal Dictionary<Vector2I, char> Details { get; } = new();
    internal Dictionary<Vector2I, Fixture> Fixtures { get; } = new();
    internal HashSet<Vector2I> Oil { get; } = new();
    internal Dictionary<Vector2I, int> Fire { get; } = new();
    internal Dictionary<Vector2I, (int Turns, char? Original)> TemporaryWater { get; } = new();
    internal int HeroPoisonTurns { get; set; }
    internal Dictionary<Enemy, int> PoisonedEnemies { get; } = new();
    internal void Clear()
    {
        TemporaryWater.Clear(); Details.Clear(); Fixtures.Clear(); Oil.Clear(); Fire.Clear();
        HeroPoisonTurns = 0; PoisonedEnemies.Clear();
    }
}
