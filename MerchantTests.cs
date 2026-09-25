using Godot;
using System;
using System.Linq;

public partial class Main
{
    void TestMerchant()
    {
        selected=0;Start(91);floor=16;Generate(true);
        if(!merchantFloor||enemies.Count!=0||items.Count!=0||!Walk(stairs)||!Walk(merchantPosition)||merchantStock.Count!=8)throw new Exception("Unsafe or incomplete merchant room");
        var originalStock=merchantStock.ToArray();var originalPlayer=player;int originalTurn=turn;
        player=merchantPosition+Vector2I.Down;Move(Vector2I.Up);
        if(player==merchantPosition||turn!=originalTurn)throw new Exception("Walked through merchant");
        Interact();if(screen!="shop")throw new Exception("Merchant interaction failed");
        HandleShop(Key.Escape);Interact();
        if(!originalStock.SequenceEqual(merchantStock))throw new Exception("Reopening shop rerolled stock");
        coins=10000;int money=coins,count=backpack.Count;
        HandleShop(Key.Enter);
        if(pendingTrade==null||confirmYes||coins!=money||backpack.Count!=count)throw new Exception("Purchase did not require explicit confirmation");
        HandleShop(Key.Enter);
        if(pendingTrade!=null||coins!=money||backpack.Count!=count)throw new Exception("Default purchase choice is not cancel");
        HandleShop(Key.Enter);var bought=pendingTrade!;int price=bought.Value;
        HandleShop(Key.Down);HandleShop(Key.Enter);
        if(coins!=money-price||backpack.Count!=count+1||bought.Quantity!=0||!ReferenceEquals(backpack[^1],bought.Gear))throw new Exception("Purchase failed");
        money=coins;CompleteTrade();if(coins!=money)throw new Exception("Transaction applied twice");
        coins=0;count=backpack.Count;shopIndex=0;HandleShop(Key.Enter);HandleShop(Key.Down);HandleShop(Key.Enter);
        if(coins!=0||backpack.Count!=count)throw new Exception("Purchase allowed without gold");
        shopSelling=true;shopIndex=0;var weapon=Weapon!;HandleShop(Key.Enter);HandleShop(Key.Escape);
        if(!ReferenceEquals(Weapon,weapon)||!backpack.Contains(weapon))throw new Exception("Cancelled sale removed equipped item");
        HandleShop(Key.Enter);HandleShop(Key.Down);HandleShop(Key.Enter);
        if(Weapon!=null||backpack.Any(g=>ReferenceEquals(g,weapon))||coins!=weapon.Value/2)throw new Exception("Equipped sale failed");
        var saleList=ShopOffers();shopIndex=saleList.FindIndex(o=>o.Gear==null&&o.Potion==0);
        int beforePotions=potions;money=coins;
        HandleShop(Key.Enter);HandleShop(Key.Down);HandleShop(Key.Enter);
        if(potions!=beforePotions-1||coins!=money+6)throw new Exception("Potion sale did not transfer exactly one");
        shopSelling=false;shopIndex=ShopOffers().FindIndex(o=>o.Gear==null&&o.Potion==0);coins=100;
        beforePotions=potions;HandleShop(Key.Enter);HandleShop(Key.Down);HandleShop(Key.Enter);
        if(potions!=beforePotions+1||coins!=88)throw new Exception("Potion purchase failed");
        for(int i=0;i<30;i++)EndTurn();
        if(enemies.Count!=0)throw new Exception("Enemies entered safe room");
        player=stairs;screen="game";int oldFloor=floor;Interact();if(floor!=oldFloor+1)throw new Exception("Safe-room stairs failed");
        screen="pause";pauseTab=4;int savedTurn=turn;money=coins;AskReturnToMenu();
        HandleExitConfirm(Key.Enter);if(screen!="pause"||coins!=money||turn!=savedTurn)throw new Exception("Default exit did not preserve run");
        AskReturnToMenu();HandleExitConfirm(Key.Escape);if(screen!="pause")throw new Exception("Exit escape failed");
        AskReturnToMenu();HandleExitConfirm(Key.Down);HandleExitConfirm(Key.Enter);if(screen!="home")throw new Exception("Confirmed exit failed");
        int encounters=0;
        for(int seed=1;seed<=200;seed++)
        {
            Start(seed);if(merchantFloor)throw new Exception("Starting floor became merchant floor");
            floor=4;Generate();if(merchantFloor){encounters++;if(enemies.Count!=0||items.Count!=0)throw new Exception("Unsafe random encounter");}
            floor=5;Generate();if(merchantFloor||enemies.All(e=>e.Glyph!='B'))throw new Exception("Merchant replaced boss floor");
        }
        if(encounters<8||encounters>35)throw new Exception("Merchant occurrence rate outside expected sample range");
        GD.Print($"MERCHANT AUDIT: {encounters}/200 eligible encounters; safe rooms, persistent stock, purchase/sale confirmation, currency, equipped sales, potions, exit cancellation passed.");
    }
}
