using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Main
{
    bool merchantFloor,shopSelling,confirmYes,exitYes;
    Vector2I merchantPosition;
    int shopIndex,merchantQuote;
    sealed class Offer
    {
        public Gear? Gear;
        public int Potion; // 0 health, 1 energy; used only without gear
        public int Quantity=1;
        public int Value=>Gear?.Value??(Potion==0?12:15);
    }
    readonly List<Offer> merchantStock=new();
    Offer? pendingTrade;
    (string Pt,string En) shopNotice=("","");
    bool MerchantEligible=>floor>1&&!IsBossFloor(floor);
    void GenerateMerchantRoom()
    {
        merchantFloor=true;enemies.Clear();items.Clear();ResetEffects();merchantStock.Clear();pendingTrade=null;
        for(int x=0;x<W;x++)for(int y=0;y<H;y++)map[x,y]='#';
        Array.Clear(seen);Array.Clear(visible);
        stairsRoom=new Rect2I(24,8,15,10);
        for(int x=stairsRoom.Position.X;x<stairsRoom.End.X;x++)for(int y=stairsRoom.Position.Y;y<stairsRoom.End.Y;y++)map[x,y]='.';
        player=new Vector2I(31,16);merchantPosition=new Vector2I(28,11);stairs=new Vector2I(36,11);map[stairs.X,stairs.Y]='>';
        for(int i=0;i<6;i++)merchantStock.Add(new Offer{Gear=new Gear((GearKind)rng.Next(8),RollRarity(floor),(floor-1)/20)});
        merchantStock.Add(new Offer{Potion=0,Quantity=rng.Next(2,5)});
        merchantStock.Add(new Offer{Potion=1,Quantity=rng.Next(1,4)});
        merchantQuote=rng.Next(4);Reveal();
        Say("Uma luz acolhedora. Voce encontrou o mercador.","A welcoming light. You found the merchant.");
    }
    void Interact()
    {
        if(merchantFloor&&Dist(player,merchantPosition)==1)
        {
            screen="shop";shopSelling=false;shopIndex=0;pendingTrade=null;shopNotice=("","");return;
        }
        Descend();
    }
    List<Offer> ShopOffers()
    {
        if(!shopSelling)return merchantStock.Where(o=>o.Quantity>0).ToList();
        var offers=backpack.Select(g=>new Offer{Gear=g}).ToList();
        if(potions>0)offers.Add(new Offer{Potion=0,Quantity=potions});
        if(energyPotions>0)offers.Add(new Offer{Potion=1,Quantity=energyPotions});
        return offers;
    }
    string OfferName(Offer offer)=>offer.Gear is Gear g?GearLabel(g):offer.Potion==0?T("Pocao de vida","Health potion"):T("Pocao de energia","Energy potion");
    int TradePrice(Offer offer)=>shopSelling?Math.Max(1,offer.Value/2):offer.Value;
    void HandleShop(Key key)
    {
        if(pendingTrade!=null)
        {
            if(key==Key.Escape){pendingTrade=null;return;}
            if(Previous(key)||Next(key)||key==Key.Left||key==Key.Right)confirmYes=!confirmYes;
            if(Confirm(key)){if(confirmYes)CompleteTrade();else pendingTrade=null;}
            return;
        }
        if(key==Key.Escape){screen="game";return;}
        if(key==Key.Left||key==Key.Right||key==Key.A||key==Key.D||key==Key.Tab){shopSelling=!shopSelling;shopIndex=0;shopNotice=("","");}
        var offers=ShopOffers();
        if(offers.Count==0)return;
        if(Previous(key))shopIndex=(shopIndex+offers.Count-1)%offers.Count;
        if(Next(key))shopIndex=(shopIndex+1)%offers.Count;
        shopIndex=Math.Clamp(shopIndex,0,offers.Count-1);
        if(Confirm(key)){pendingTrade=offers[shopIndex];confirmYes=false;}
    }
    void CompleteTrade()
    {
        var offer=pendingTrade;if(offer==null)return;
        int price=TradePrice(offer);
        if(shopSelling)
        {
            if(offer.Gear is Gear gear)
            {
                int index=backpack.FindIndex(g=>ReferenceEquals(g,gear));
                if(index<0){pendingTrade=null;return;}
                for(int i=0;i<3;i++)if(ReferenceEquals(equipped[i],gear))equipped[i]=null;
                backpack.RemoveAt(index);merchantStock.Add(new Offer{Gear=gear});aiming=false;
            }
            else
            {
                if((offer.Potion==0?potions:energyPotions)<=0){pendingTrade=null;return;}
                if(offer.Potion==0)potions--;else energyPotions--;
                var existing=merchantStock.FirstOrDefault(o=>o.Gear==null&&o.Potion==offer.Potion);
                if(existing==null)merchantStock.Add(new Offer{Potion=offer.Potion});else existing.Quantity++;
            }
            coins+=price;
            shopNotice=($"Venda concluida. +{price} ouro.",$"Sold. +{price} gold.");
        }
        else
        {
            if(!merchantStock.Contains(offer)||offer.Quantity<=0){pendingTrade=null;return;}
            if(coins<price){shopNotice=("Ouro insuficiente.","Not enough gold.");pendingTrade=null;return;}
            coins-=price;offer.Quantity--;
            if(offer.Gear is Gear gear)backpack.Add(gear);
            else if(offer.Potion==0)potions++;else energyPotions++;
            shopNotice=($"Compra concluida. -{price} ouro.",$"Purchased. -{price} gold.");
        }
        Say(shopNotice.Pt,shopNotice.En);pendingTrade=null;shopIndex=Math.Max(0,Math.Min(shopIndex,ShopOffers().Count-1));merchantQuote=(merchantQuote+1)%4;
    }
    string MerchantSpeech()=>T(new[]{"O Abismo cobra caro. Eu aceito moedas.","Aco firme, frascos cheios. Escolha bem.","Aqui, ate as sombras respeitam a tregua.","Volte vivo. Bons clientes sao raros."}[merchantQuote],new[]{"The Abyss asks a price. I take coins.","Steady steel, full bottles. Choose well.","Here, even shadows honor the truce.","Come back alive. Good customers are rare."}[merchantQuote]);
    void DrawShop()
    {
        Text(32,96,T("O MERCADOR DO ABISMO","THE ABYSS MERCHANT"),gold,24);
        Portrait(32,158,AsciiArt.Merchant,T("MERCADOR","MERCHANT"),teal);
        Text(32,490,T($"OURO {coins}",$"GOLD {coins}"),gold,21);
        // Short lines keep the merchant's speech inside its column.
        string speech=MerchantSpeech();int row=0;
        while(speech.Length>0)
        {
            int cut=Math.Min(28,speech.Length);if(cut<speech.Length){int space=speech.LastIndexOf(' ',cut-1);if(space>0)cut=space;}
            Text(32,535+row++*24,speech[..cut],ink,14);speech=speech[cut..].TrimStart();
        }
        Text(340,145,(shopSelling?"  ":"> ")+T("COMPRAR","BUY"),shopSelling?dim:teal,21);
        Text(640,145,(shopSelling?"> ":"  ")+T("VENDER","SELL"),shopSelling?teal:dim,21);
        if(pendingTrade is Offer pending)
        {
            Frame(335,183,96,23,dim,15,20);
            Text(358,227,shopSelling?T("CONFIRMAR VENDA?","CONFIRM SALE?"):T("CONFIRMAR COMPRA?","CONFIRM PURCHASE?"),gold,23);
            Text(358,275,OfferName(pending),pending.Gear is Gear g?RarityColor(g.Quality):ink,20);
            Text(358,319,T($"Quantidade: 1  |  Valor: {TradePrice(pending)} ouro",$"Quantity: 1  |  Price: {TradePrice(pending)} gold"),ink,19);
            if(shopSelling&&pending.Gear is Gear worn&&equipped.Any(g=>ReferenceEquals(g,worn)))
                Text(358,365,T("Este item esta equipado e sera removido.","This item is equipped and will be removed."),red,17);
            Text(358,436,(confirmYes?"  ":"> ")+T("NAO, CANCELAR","NO, CANCEL"),confirmYes?dim:teal,21);
            Text(358,492,(confirmYes?"> ":"  ")+T("SIM, CONFIRMAR","YES, CONFIRM"),confirmYes?teal:dim,21);
            Footer("[SETAS] escolher   [ENTER] confirmar   [ESC] cancelar","[ARROWS] choose   [ENTER] confirm   [ESC] cancel");return;
        }
        var offers=ShopOffers();shopIndex=Math.Clamp(shopIndex,0,Math.Max(0,offers.Count-1));int page=shopIndex/7;
        if(offers.Count==0)Text(350,215,T("Nenhum item disponivel.","No items available."),dim,19);
        for(int i=page*7;i<Math.Min(offers.Count,page*7+7);i++)
        {
            var o=offers[i];Text(345,204+(i-page*7)*35,(i==shopIndex?"> ":"  ")+OfferName(o)+$" x{o.Quantity} | {TradePrice(o)} "+T("ouro","gold"),o.Gear is Gear g?RarityColor(g.Quality):ink,17);
        }
        Text(345,462,T($"PAGINA {page+1}/{Math.Max(1,(offers.Count+6)/7)}",$"PAGE {page+1}/{Math.Max(1,(offers.Count+6)/7)}"),dim,14);
        if(offers.Count>0)
        {
            var o=offers[shopIndex];
            if(o.Gear is Gear g)
            {
                string classes=string.Join(" / ",Enumerable.Range(0,4).Where(g.Allows).Select(ClassName));
                Text(345,501,T($"Requer nivel {g.RequiredLevel}: ",$"Requires level {g.RequiredLevel}: ")+classes,level>=g.RequiredLevel&&g.Allows(selected)?teal:red,15);
                string stats=GearStats(g);
                Text(345,535,stats,ink,16);Text(345,568,GearEffect(g),ink,16);
                if(!g.Allows(selected)||level<g.RequiredLevel)Text(345,600,T("Pode comprar e guardar; ainda nao pode equipar.","You may buy and keep it, but cannot equip it yet."),dim,15);
            }
            else Text(345,510,o.Potion==0?T("Cura 2d10 PV (2-20).","Heals 2d10 HP (2-20)."):T("Restaura 2d6 energia (2-12).","Restores 2d6 energy (2-12)."),ink,18);
        }
        Text(345,668,T(shopNotice.Pt,shopNotice.En),gold,17);
        Footer("[A D / TAB] comprar/vender   [CIMA/BAIXO] item   [ENTER] negociar   [ESC] sair","[A D / TAB] buy/sell   [UP/DOWN] item   [ENTER] trade   [ESC] leave");
    }
    void AskReturnToMenu(){exitYes=false;screen="confirm_exit";}
    void HandleExitConfirm(Key key)
    {
        if(key==Key.Escape){screen="pause";return;}
        if(Previous(key)||Next(key)||key==Key.Left||key==Key.Right)exitYes=!exitYes;
        if(Confirm(key)){screen=exitYes?"home":"pause";if(exitYes)menuIndex=0;}
    }
    void DrawExitConfirm()
    {
        DrawAsciiImage(32,139,AsciiArt.Camp,510,510,new Color("fff6df"),2);
        Text(600,223,T("VOLTAR AO MENU PRINCIPAL?","RETURN TO THE MAIN MENU?"),gold,23);
        Lines(600,290,T("A expedicao sera encerrada.\nSeu progresso nao sera salvo.","The expedition will end.\nYour progress will not be saved."),ink,20,35);
        Text(600,410,(exitYes?"  ":"> ")+T("NAO, CONTINUAR","NO, CONTINUE"),exitYes?dim:teal,22);
        Text(600,474,(exitYes?"> ":"  ")+T("SIM, VOLTAR AO MENU","YES, RETURN TO MENU"),exitYes?teal:dim,22);
        Footer("[SETAS] escolher   [ENTER] confirmar   [ESC] cancelar","[ARROWS] choose   [ENTER] confirm   [ESC] cancel");
    }
}
