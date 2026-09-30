using System;
using System.Linq;

namespace Abyss.Application;
internal sealed class InventoryService
{
    private readonly SoundEffects sounds;
    private readonly ITurnScheduler turns;
    private readonly ExpeditionJournal expeditionJournal;
    private readonly InventoryState inventoryState;
    private readonly Localization localization;
    private readonly MenuState menuState;
    private readonly PlayerState playerState;
    private readonly RunState runState;
    private readonly RandomStream random;
    internal InventoryService(ITurnScheduler turns, ExpeditionJournal expeditionJournal, InventoryState inventoryState, Localization localization, MenuState menuState, PlayerState playerState, RunState runState, RandomStream random, SoundEffects sounds)
    {
        this.turns = turns;
        this.sounds = sounds;
        this.expeditionJournal = expeditionJournal;
        this.inventoryState = inventoryState;
        this.localization = localization;
        this.menuState = menuState;
        this.playerState = playerState;
        this.runState = runState;
        this.random = random;
    }

    internal void ResetInventory()
    {
        inventoryState.Backpack.Clear();
        Array.Clear(inventoryState.Equipped);
        inventoryState.Potions = 5;
        inventoryState.SpareTorches = 0;
        inventoryState.TorchFuel = 100;
        inventoryState.TorchEquipped = true;
        inventoryState.EnergyPotions = menuState.InventoryIndex = 0;
        menuState.InventoryNotice = ("", "");
        var starter = new Gear(new[] { GearKind.Sword, GearKind.Staff, GearKind.Bow, GearKind.Dagger }[playerState.ClassIndex], Rarity.Common);
        inventoryState.Backpack.Add(starter);
        inventoryState.Equipped[0] = starter;
    }

    internal void InventoryMessage(string pt, string en)
    {
        menuState.InventoryNotice = (pt, en);
    }

    internal void ToggleGear(Gear gear)
    {
        int slot = (int)gear.Slot;
        if (!inventoryState.Backpack.Any(g => ReferenceEquals(g, gear)))
            return;
        if (ReferenceEquals(inventoryState.Equipped[slot], gear))
            inventoryState.Equipped[slot] = null;
        else
        {
            if (!gear.Allows(playerState.ClassIndex))
            {
                InventoryMessage("Sua classe nao pode usar este item.", "Your class cannot use this item.");
                return;
            }

            if (playerState.Level < gear.RequiredLevel)
            {
                InventoryMessage($"Requer nivel {gear.RequiredLevel}.", $"Requires level {gear.RequiredLevel}.");
                return;
            }

            inventoryState.Equipped[slot] = gear;
        }

        sounds.Play("equip");
        runState.IsAiming = false;
        expeditionJournal.Say($"Equipamento alterado: {localization.GearNameFor(gear, false)}.", $"Equipment changed: {localization.GearNameFor(gear, true)}.");
        runState.Screen = "game";
        turns.EndTurn();
    }

    internal void DrinkEnergy()
    {
        if (inventoryState.EnergyPotions == 0 || playerState.Energy == playerState.MaxEnergy)
        {
            InventoryMessage("Sem pocao ou energia ja cheia.", "No potion or energy already full.");
            return;
        }

        int roll = random.Generator.Next(1, 7) + random.Generator.Next(1, 7), healing = Math.Min(playerState.MaxEnergy - playerState.Energy, roll);
        sounds.Play("potion");
        playerState.Energy += healing;
        inventoryState.EnergyPotions--;
        runState.Screen = "game";
        expeditionJournal.Say($"Pocao de energia: 2d6 = {roll}. +{healing} EN.", $"Energy potion: 2d6 = {roll}. +{healing} EN.");
        turns.EndTurn();
    }
}
