using System;
using System.Linq;
using System.Collections.Generic;

namespace Abyss.Application;
internal sealed class MerchantService
{
    private readonly ExpeditionJournal expeditionJournal;
    private readonly InventoryState inventoryState;
    private readonly MenuState menuState;
    private readonly MerchantState merchantState;
    private readonly PlayerState playerState;
    private readonly RunState runState;
    internal MerchantService(ExpeditionJournal expeditionJournal, InventoryState inventoryState, MenuState menuState, MerchantState merchantState, PlayerState playerState, RunState runState)
    {
        this.expeditionJournal = expeditionJournal;
        this.inventoryState = inventoryState;
        this.menuState = menuState;
        this.merchantState = merchantState;
        this.playerState = playerState;
        this.runState = runState;
    }

    internal List<Offer> ShopOffers()
    {
        if (!menuState.ShopSelling)
            return merchantState.MerchantStock.Where(o => o.Quantity > 0).ToList();
        var offers = inventoryState.Backpack.Select(g => new Offer { Gear = g }).ToList();
        if (inventoryState.Potions > 0)
            offers.Add(new Offer { Potion = 0, Quantity = inventoryState.Potions });
        if (inventoryState.EnergyPotions > 0)
            offers.Add(new Offer { Potion = 1, Quantity = inventoryState.EnergyPotions });
        return offers;
    }

    internal int TradePrice(Offer offer) => menuState.ShopSelling ? Math.Max(1, offer.Value / 2) : offer.Value;
    internal void CompleteTrade()
    {
        var offer = menuState.PendingTrade;
        if (offer == null)
            return;
        int price = TradePrice(offer);
        if (menuState.ShopSelling)
        {
            if (offer.Gear is Gear gear)
            {
                int index = inventoryState.Backpack.FindIndex(g => ReferenceEquals(g, gear));
                if (index < 0)
                {
                    menuState.PendingTrade = null;
                    return;
                }

                for (int i = 0; i < 3; i++)
                    if (ReferenceEquals(inventoryState.Equipped[i], gear))
                        inventoryState.Equipped[i] = null;
                inventoryState.Backpack.RemoveAt(index);
                merchantState.MerchantStock.Add(new Offer { Gear = gear });
                runState.IsAiming = false;
            }
            else
            {
                if ((offer.Potion == 0 ? inventoryState.Potions : inventoryState.EnergyPotions) <= 0)
                {
                    menuState.PendingTrade = null;
                    return;
                }

                if (offer.Potion == 0)
                    inventoryState.Potions--;
                else
                    inventoryState.EnergyPotions--;
                var existing = merchantState.MerchantStock.FirstOrDefault(o => o.Gear == null && o.Potion == offer.Potion);
                if (existing == null)
                    merchantState.MerchantStock.Add(new Offer { Potion = offer.Potion });
                else
                    existing.Quantity++;
            }

            playerState.Gold += price;
            menuState.ShopNotice = ($"Venda concluida. +{price} ouro.", $"Sold. +{price} gold.");
        }
        else
        {
            if (!merchantState.MerchantStock.Contains(offer) || offer.Quantity <= 0)
            {
                menuState.PendingTrade = null;
                return;
            }

            if (playerState.Gold < price)
            {
                menuState.ShopNotice = ("Ouro insuficiente.", "Not enough gold.");
                menuState.PendingTrade = null;
                return;
            }

            playerState.Gold -= price;
            offer.Quantity--;
            if (offer.Gear is Gear gear)
                inventoryState.Backpack.Add(gear);
            else if (offer.Potion == 0)
                inventoryState.Potions++;
            else
                inventoryState.EnergyPotions++;
            menuState.ShopNotice = ($"Compra concluida. -{price} ouro.", $"Purchased. -{price} gold.");
        }

        expeditionJournal.Say(menuState.ShopNotice.Pt, menuState.ShopNotice.En);
        menuState.PendingTrade = null;
        menuState.ShopIndex = Math.Max(0, Math.Min(menuState.ShopIndex, ShopOffers().Count - 1));
        menuState.MerchantQuote = (menuState.MerchantQuote + 1) % 4;
    }
}
