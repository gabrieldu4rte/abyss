using System.Collections.Generic;
namespace Abyss.Domain;
internal sealed class BestiaryState
{
    private readonly Dictionary<string, int> defeated = new();
    internal IReadOnlyDictionary<string, int> Defeated => defeated;
    internal int Count(string species) => defeated.GetValueOrDefault(species);
    internal void Restore(string species, int count) => defeated[species] = count;
    internal void Record(string species) => defeated[species] = System.Math.Min(int.MaxValue - 1, Count(species)) + 1;
}
