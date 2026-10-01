using static Abyss.Rules.TabletopRules;

namespace Abyss.Domain;
internal sealed record Gear(GearKind Kind, Rarity Quality, int Grade = 0, ItemId Special = ItemId.None)
{
    public GearSlot Slot => Kind <= GearKind.Bow ? GearSlot.Weapon : Kind == GearKind.Amulet ? GearSlot.Accessory : GearSlot.Armor;
    public int RequiredLevel => new[]
    {
        1,
        3,
        6,
        10
    }[(int)Quality] + Grade;
    public int Power => (int)Quality + Grade;
    public int Sides => (Kind == GearKind.Sword ? 8 : 6) + 2 * (int)Quality;
    public int ArmorClass => Kind switch
    {
        GearKind.Plate => 14 + Power / 2,
        GearKind.Leather => 12 + Power / 2,
        GearKind.Robe => 11 + Power / 2,
        _ => 10
    };
    public Attributes AttributeBonus => Kind switch
    {
        GearKind.Plate => new(1 + Power / 2, 0, 1 + Power, 0),
        GearKind.Leather => new(0, 1 + Power, 0, 0),
        GearKind.Robe => new(0, 0, 0, 1 + Power),
        GearKind.Amulet => new(1 + Power, 1 + Power, 1 + Power, 1 + Power),
        _ => new(0, 0, 0, 0)};
    public int Value => (Slot == GearSlot.Weapon ? 18 : Slot == GearSlot.Armor ? 24 : 20) * (1 + (int)Quality * 3) + Grade * 12;

    public bool Allows(int hero) => Special is ItemId.DeepBreathMantle or ItemId.ShadowLegendsHood || Kind switch
    {
        GearKind.Sword or GearKind.Dagger => hero is 0 or 3,
        GearKind.Staff or GearKind.Robe => hero == 1,
        GearKind.Bow => hero == 2,
        GearKind.Plate => hero == 0,
        _ => true
    };
}
