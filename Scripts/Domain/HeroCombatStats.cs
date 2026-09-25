using System;
using static Abyss.Rules.TabletopRules;

namespace Abyss.Domain;
internal sealed class HeroCombatStats
{
    private readonly PlayerState player;
    private readonly InventoryState inventory;
    internal HeroCombatStats(PlayerState player, InventoryState inventory)
    {
        this.player = player;
        this.inventory = inventory;
    }

    internal bool CanMelee => inventory.Weapon == null || player.ClassIndex is 0 or 3;
    internal bool CanShoot => (player.ClassIndex == 1 && inventory.Weapon?.Kind == GearKind.Staff) || (player.ClassIndex == 2 && inventory.Weapon?.Kind == GearKind.Bow);
    internal int Proficiency => 2 + (player.Level - 1) / 4;
    internal int PrimaryModifier => player.ClassIndex == 0 ? EffectiveAttributes.Str : player.ClassIndex == 1 ? EffectiveAttributes.Int : EffectiveAttributes.Dex;
    internal int MeleeModifier => inventory.Weapon?.Kind switch
    {
        GearKind.Sword => EffectiveAttributes.Str,
        GearKind.Dagger or GearKind.Bow => EffectiveAttributes.Dex,
        GearKind.Staff => EffectiveAttributes.Int,
        _ => PrimaryModifier
    };
    internal int Defense => inventory.EquippedArmorClass + EffectiveAttributes.Con;
    internal int MeleeBonus => Proficiency + MeleeModifier;
    internal int SpellBonus => Proficiency + PrimaryModifier;
    internal int ShotCost => 0;
    internal int AbilityCost => Math.Max(player.ClassIndex == 1 ? 4 : 3, new[] { 7, 10, 8, 8 }[player.ClassIndex] - (player.Level - 1) / 2);
    internal int XpToNext => 30 + 20 * (player.Level - 1);
    internal DamageDice MeleeDice => inventory.Weapon is Gear w ? new(1, w.Sides, MeleeModifier + w.Power) : new(1, 2, 0);
    internal DamageDice ShotDice => inventory.Weapon is Gear w ? new(1, w.Sides, PrimaryModifier + w.Power) : new(1, 2, 0);
    internal DamageDice AbilityDice => new(2, player.ClassIndex == 2 ? 8 : 6, PrimaryModifier + (player.Level - 1) / 2 + inventory.WeaponBonus);
    internal int AbilityRange => new[]
    {
        2,
        5,
        10,
        3
    }[player.ClassIndex];

    internal Attributes EffectiveAttributes
    {
        get
        {
            var result = player.Attributes;
            foreach (var gear in inventory.Equipped)
            {
                if (gear == null)
                    continue;
                var bonus = gear.AttributeBonus;
                result = new(result.Strength + bonus.Strength, result.Dexterity + bonus.Dexterity, result.Constitution + bonus.Constitution, result.Intelligence + bonus.Intelligence);
            }

            return result;
        }
    }
}
