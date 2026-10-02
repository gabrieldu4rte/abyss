using System;
using System.Collections.Generic;
namespace Abyss.Rules;
internal static class EnemyCatalog
{
    internal static Biome BiomeAt(int depth) => (Biome)(GameRules.CycleIndex(depth) % 4);
    internal static string Roster(int depth) => BiomeAt(depth) switch { Biome.Ruins => "sgvq", Biome.Cistern => "rdle", Biome.FungalCaves => "fjmb", _ => "hika" };
    internal static readonly IReadOnlyDictionary<char, EnemyProfile> Common = new Dictionary<char, EnemyProfile>
    {
        ['q'] = new("ruin_gargoyle", 10, 12, 4, 5, new(12, 8, 10, 4)),
        ['e'] = new("cistern_eel", 6, 10, 3, 3, new(8, 12, 8, 10), CombatAttribute.Dexterity),
        ['b'] = new("spore_bloom", 7, 10, 3, 3, new(8, 8, 10, 12), CombatAttribute.Intelligence),
        ['a'] = new("forge_salamander", 9, 11, 4, 4, new(12, 10, 10, 6)),
        ['s'] = new("skeleton", 6, 11, 4, 3, new(10, 10, 10, 4)),
        ['g'] = new("goblin", 8, 10, 4, 4, new(10, 12, 10, 8), CombatAttribute.Dexterity),
        ['v'] = new("revenant", 9, 11, 4, 4, new(12, 8, 10, 8)),
        ['r'] = new("rat", 4, 10, 3, 2, new(8, 10, 8, 2), CombatAttribute.Dexterity),
        ['d'] = new("drowned", 8, 10, 4, 3, new(12, 8, 10, 6)),
        ['l'] = new("leech", 5, 10, 3, 2, new(8, 12, 8, 2), CombatAttribute.Dexterity),
        ['f'] = new("sporeling", 5, 10, 3, 2, new(8, 10, 8, 12), CombatAttribute.Intelligence),
        ['j'] = new("cave_crawler", 6, 11, 4, 3, new(10, 12, 8, 2), CombatAttribute.Dexterity),
        ['m'] = new("myconid", 9, 10, 4, 4, new(12, 8, 12, 8)),
        ['h'] = new("cinder_hound", 6, 10, 4, 3, new(10, 12, 8, 4), CombatAttribute.Dexterity),
        ['i'] = new("ember_imp", 5, 10, 3, 2, new(8, 12, 8, 12), CombatAttribute.Intelligence),
        ['k'] = new("forged_sentinel", 9, 12, 4, 4, new(12, 8, 10, 6))
    };
    private static readonly EnemyProfile[] Wardens = {
        new("warden", 24, 13, 3, 18, new(12, 10, 12, 12)),
        new("tide_warden", 24, 13, 3, 18, new(12, 10, 12, 14)),
        new("spore_warden", 24, 12, 3, 18, new(10, 10, 12, 14)),
        new("forge_warden", 24, 14, 3, 18, new(14, 8, 12, 12))
    };
    internal static EnemyProfile Get(char glyph, int depth) => glyph == 'B' ? Wardens[(int)BiomeAt(depth)] : Common.TryGetValue(glyph, out var profile) ? profile : throw new ArgumentException($"Unknown enemy glyph: {glyph}");
}
