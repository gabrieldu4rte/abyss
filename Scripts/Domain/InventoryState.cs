using System.Collections.Generic;

namespace Abyss.Domain;
internal sealed class InventoryState
{
    internal int Potions { get; set; }
    internal List<Gear> Backpack { get; } = new();
    internal Gear? [] Equipped { get; } = new Gear? [3];
    internal int EnergyPotions { get; set; }
    internal Gear? Weapon => Equipped[0];
    internal int EquippedArmorClass => Equipped[1]?.ArmorClass ?? 10;
    internal int WeaponBonus => Weapon?.Power ?? 0;
    internal int DamageReduction => Equipped[1]?.Quality switch
    {
        Rarity.Epic => 1,
        Rarity.Legendary => 2,
        _ => 0
    };
}
