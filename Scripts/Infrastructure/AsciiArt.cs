using Godot;
using System;
using System.Collections.Generic;

namespace Abyss.Infrastructure;
public static class AsciiArt
{
    public static readonly Dictionary<string, string> ToneMaps = new();
    static string Read(string name)
    {
        string path = "res://Art/" + name + ".txt";
        string text = FileAccess.GetFileAsString(path);
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Missing ASCII illustration: " + path);
        ToneMaps[text] = FileAccess.GetFileAsString("res://Art/" + name + ".tone");
        return text;
    }

    public static readonly string[] Heroes =
    {
        Read("warrior"),
        Read("mage"),
        Read("archer"),
        Read("rogue")
    };
    public static readonly string[] CriticalHeroes =
    {
        Read("warrior_critical"),
        Read("mage_critical"),
        Read("archer_critical"),
        Read("rogue_critical")
    };
    public static readonly string Merchant = Read("merchant");
    public static readonly string Rat = Read("rat"), Skeleton = Read("skeleton"), Goblin = Read("goblin"), Warden = Read("warden"), Unknown = Read("unknown");
    public static readonly string Tower = Read("tower"), Camp = Read("camp"), Globe = Read("globe"), Book = Read("book"), Grave = Read("grave"), Crown = Read("crown"), Torch = Read("torch");
    private static readonly Dictionary<string, string> CreaturePortraits = new()
    {
        ["rat"] = Rat, ["skeleton"] = Skeleton, ["goblin"] = Goblin, ["warden"] = Warden,
        ["revenant"] = Read("revenant"),
        ["drowned"] = Read("drowned"),
        ["leech"] = Read("leech"),
        ["sporeling"] = Read("sporeling"),
        ["cave_crawler"] = Read("cave_crawler"),
        ["myconid"] = Read("myconid"),
        ["cinder_hound"] = Read("cinder_hound"),
        ["ember_imp"] = Read("ember_imp"),
        ["forged_sentinel"] = Read("forged_sentinel"),
        ["tide_warden"] = Read("tide_warden"),
        ["spore_warden"] = Read("spore_warden"),
        ["forge_warden"] = Read("forge_warden"),
    };
    public static string Enemy(char glyph) => glyph == 'B' || EnemyCatalog.Common.ContainsKey(glyph) ? CreaturePortraits[EnemyCatalog.Get(glyph, 1).ArtKey] : Unknown;
    internal static string Creature(string key) => CreaturePortraits[key];
    internal static string Enemy(Enemy? enemy) => enemy == null ? Unknown : CreaturePortraits[enemy.Profile.ArtKey];
}
