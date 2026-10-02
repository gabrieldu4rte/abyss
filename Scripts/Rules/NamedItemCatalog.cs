using System;
using System.Linq;
namespace Abyss.Rules;
internal static class NamedItemCatalog
{
    internal static Gear Create(ItemId id, int grade = 0) => new(id switch {
        ItemId.ExplorerBlade => GearKind.Sword,
        ItemId.VigorBracers => GearKind.Leather,
        ItemId.ContinuousFlameBuckle => GearKind.Amulet,
        ItemId.InvestigatorMantle => GearKind.Robe,
        ItemId.EmberStaff => GearKind.Staff,
        ItemId.TwilightBow => GearKind.Bow,
        ItemId.HunterGreedAmulet => GearKind.Amulet,
        ItemId.ThickRubberBoots => GearKind.Leather,
        ItemId.ConductiveCrossbow => GearKind.Bow,
        ItemId.FungalSovereignCrown => GearKind.Robe,
        ItemId.EternalForgeRobe => GearKind.Robe,
        ItemId.SparkSword or ItemId.SpellforgeBlade => GearKind.Sword,
        ItemId.VenomBow or ItemId.RevengeBow => GearKind.Bow,
        ItemId.InsulatingLeather => GearKind.Leather,
        ItemId.FluidStaff => GearKind.Staff,
        ItemId.DeepBreathMantle or ItemId.ShadowLegendsHood => GearKind.Robe,
        ItemId.ExecutionerBlade => GearKind.Dagger,
        _ => GearKind.Amulet
    }, Quality(id), grade, id);
    private static Rarity Quality(ItemId id) => id switch {
        ItemId.ExplorerBlade => Rarity.Rare,
        ItemId.VigorBracers => Rarity.Rare,
        ItemId.ContinuousFlameBuckle => Rarity.Rare,
        ItemId.InvestigatorMantle => Rarity.Rare,
        ItemId.EmberStaff => Rarity.Epic,
        ItemId.TwilightBow => Rarity.Epic,
        ItemId.HunterGreedAmulet => Rarity.Epic,
        ItemId.ThickRubberBoots => Rarity.Epic,
        ItemId.ConductiveCrossbow => Rarity.Legendary,
        ItemId.FungalSovereignCrown => Rarity.Legendary,
        ItemId.EternalForgeRobe => Rarity.Legendary,
        _ => id <= ItemId.CampRing ? Rarity.Rare : id <= ItemId.ExecutionerBlade ? Rarity.Epic : Rarity.Legendary
    };
    internal static ItemId[] OfRarity(Rarity rarity) => Enum.GetValues<ItemId>().Where(i => i != ItemId.None && Create(i).Quality == rarity).ToArray();
}
