using System;
using System.Linq;
namespace Abyss.Rules;
internal static class NamedItemCatalog
{
    internal static Gear Create(ItemId id, int grade = 0) => new(id switch {
        ItemId.SparkSword or ItemId.SpellforgeBlade => GearKind.Sword,
        ItemId.VenomBow or ItemId.RevengeBow => GearKind.Bow,
        ItemId.InsulatingLeather => GearKind.Leather,
        ItemId.FluidStaff => GearKind.Staff,
        ItemId.DeepBreathMantle or ItemId.ShadowLegendsHood => GearKind.Robe,
        ItemId.ExecutionerBlade => GearKind.Dagger,
        _ => GearKind.Amulet
    }, id <= ItemId.CampRing ? Rarity.Rare : id <= ItemId.ExecutionerBlade ? Rarity.Epic : Rarity.Legendary, grade, id);
    internal static ItemId[] OfRarity(Rarity rarity) => Enum.GetValues<ItemId>().Where(i => i != ItemId.None && Create(i).Quality == rarity).ToArray();
}
