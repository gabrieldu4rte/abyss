namespace Abyss.Rules;
internal static class TrapRules
{
    internal static Fixture ForBiome(Biome biome) => biome switch
    {
        Biome.Ruins => Fixture.SpikeTrap,
        Biome.Cistern => Fixture.ShockTrap,
        Biome.FungalCaves => Fixture.PoisonTrap,
        _ => Fixture.FlameTrap
    };
    internal static bool IsTrap(Fixture fixture) => fixture is Fixture.SpikeTrap or Fixture.ShockTrap or Fixture.PoisonTrap or Fixture.FlameTrap;
}
