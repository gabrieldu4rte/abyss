using System;

namespace Abyss.Application;
internal sealed class LootService
{
    private readonly DungeonState dungeonState;
    private readonly ExpeditionJournal expeditionJournal;
    private readonly InventoryState inventoryState;
    private readonly Localization localization;
    private readonly RandomStream random;
    internal LootService(DungeonState dungeonState, ExpeditionJournal expeditionJournal, InventoryState inventoryState, Localization localization, RandomStream random)
    {
        this.dungeonState = dungeonState;
        this.expeditionJournal = expeditionJournal;
        this.inventoryState = inventoryState;
        this.localization = localization;
        this.random = random;
    }

    internal Rarity RollRarity(int depth, bool guardian = false)
    {
        int cycle = GameRules.CycleIndex(depth), roll = random.Generator.Next(100);
        int legendary = guardian ? Math.Min(35, 2 + cycle * 3) : Math.Min(15, cycle);
        int epic = guardian ? Math.Min(45, 10 + cycle * 3) : Math.Min(25, cycle * 2);
        int rare = Math.Min(40, 15 + cycle * 2);
        return roll < legendary ? Rarity.Legendary : roll < legendary + epic ? Rarity.Epic : guardian || roll < legendary + epic + rare ? Rarity.Rare : Rarity.Common;
    }

    internal Gear DropEquipment(int depth, bool guardian = false)
    {
        var gear = new Gear((GearKind)random.Generator.Next(8), RollRarity(depth, guardian), (depth - 1) / 20);
        inventoryState.Backpack.Add(gear);
        return gear;
    }

    internal void OpenChest()
    {
        int drops = 1 + (random.Generator.NextDouble() < GameRules.ExtraChestDropChance(dungeonState.Floor) ? 1 : 0);
        for (int i = 0; i < drops; i++)
        {
            if (random.Generator.NextDouble() < .70)
            {
                var gear = DropEquipment(dungeonState.Floor);
                expeditionJournal.Say($"Bau: {localization.GearNameFor(gear, false)}. [I] inventario.", $"Chest: {localization.GearNameFor(gear, true)}. [I] inventory.");
            }
            else if (random.Generator.Next(3) == 0)
            {
                inventoryState.Potions++;
                expeditionJournal.Say("Bau: +1 pocao de vida.", "Chest: +1 health potion.");
            }
            else
            {
                if (random.Generator.Next(2) == 0)
                {
                    inventoryState.EnergyPotions++;
                    expeditionJournal.Say("Bau: +1 pocao de energia.", "Chest: +1 energy potion.");
                }
                else
                {
                    inventoryState.SpareTorches++;
                    expeditionJournal.Say("Bau: +1 tocha.", "Chest: +1 torch.");
                }
            }
        }
    }

    internal int RollEnemyGold(char glyph, int depth)
    {
        int cycle = GameRules.CycleIndex(depth);
        if (glyph == 'B')
            return random.Generator.Next(18 + cycle * 8, 29 + cycle * 8);
        return random.Generator.Next(2) == 0 ? 0 : random.Generator.Next(1 + cycle, 3 + cycle * 2);
    }
}
