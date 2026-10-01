using System.Collections.Generic;

namespace Abyss.Domain;
internal sealed class InventoryState
{
    internal HashSet<ItemId> IssuedLegendaries { get; } = new();
    internal bool Has(ItemId id) => System.Array.Exists(Equipped, gear => gear?.Special == id);
    internal int TorchLifetime => Has(ItemId.CampRing) ? 120 : 100;
    private double torchRemaining;
    internal void BurnTorchTurn() => torchRemaining = System.Math.Max(0, torchRemaining - 1.0 / TorchLifetime);
    internal int SpareTorches { get; set; }
    internal int TorchFuel
    {
        get => (int)System.Math.Ceiling(torchRemaining * TorchLifetime - .0000001);
        set => torchRemaining = System.Math.Clamp((double)value / TorchLifetime, 0, 1);
    }
    internal bool TorchEquipped { get; set; }
    internal bool HasLight => TorchEquipped && TorchFuel > 0;
    internal int TorchCount => SpareTorches + (TorchFuel > 0 ? 1 : 0);
    internal bool LightReserve()
    {
        if (TorchFuel > 0 || SpareTorches <= 0) return false;
        SpareTorches--;
        TorchFuel = TorchLifetime;
        TorchEquipped = true;
        return true;
    }
    internal int Potions { get; set; }
    internal List<Gear> Backpack { get; } = new();
    internal Gear? [] Equipped { get; } = new Gear? [3];
    internal int EnergyPotions { get; set; }
    internal Gear? Weapon => Equipped[0];
    internal int EquippedArmorClass => Equipped[1]?.ArmorClass ?? 10;
    internal int WeaponBonus => Weapon?.Power ?? 0;
    internal int DamageReduction => Equipped[1]?.Special != ItemId.None ? 0 : Equipped[1]?.Quality switch
    {
        Rarity.Epic => 1,
        Rarity.Legendary => 2,
        _ => 0
    };
}
