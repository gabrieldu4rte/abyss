using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using static TabletopRules;

public partial class Main
{
    enum GearSlot { Weapon, Armor, Accessory }
    enum Rarity { Common, Rare, Epic, Legendary }
    enum GearKind { Sword, Dagger, Staff, Bow, Plate, Leather, Robe, Amulet }
    sealed record Gear(GearKind Kind,Rarity Quality,int Grade=0)
    {
        public GearSlot Slot=>Kind<=GearKind.Bow?GearSlot.Weapon:Kind==GearKind.Amulet?GearSlot.Accessory:GearSlot.Armor;
        public int RequiredLevel=>new[]{1,3,6,10}[(int)Quality]+Grade;
        public int Power=>(int)Quality+Grade;
        public int Sides=>(Kind==GearKind.Sword?8:6)+2*(int)Quality;
        public int ArmorClass=>Kind switch {GearKind.Plate=>14+Power/2,GearKind.Leather=>12+Power/2,GearKind.Robe=>11+Power/2,_=>10};
        public Attributes AttributeBonus=>Kind switch {
            GearKind.Plate=>new(1+Power/2,0,1+Power,0),
            GearKind.Leather=>new(0,1+Power,0,0),
            GearKind.Robe=>new(0,0,0,1+Power),
            GearKind.Amulet=>new(1+Power,1+Power,1+Power,1+Power),_=>new(0,0,0,0)};
        public int Value=>(Slot==GearSlot.Weapon?18:Slot==GearSlot.Armor?24:20)*(1+(int)Quality*3)+Grade*12;
        public bool Allows(int hero)=>Kind switch {GearKind.Sword or GearKind.Dagger=>hero is 0 or 3,GearKind.Staff or GearKind.Robe=>hero==1,GearKind.Bow=>hero==2,GearKind.Plate=>hero==0,_=>true};
    }
    readonly List<Gear> backpack=new();
    readonly Gear?[] equipped=new Gear?[3];
    int energyPotions,inventoryIndex;
    (string Pt,string En) inventoryNotice=("","");
    Gear? Weapon=>equipped[0];
    int EquippedArmorClass=>equipped[1]?.ArmorClass??10;
    Attributes EffectiveAttributes
    {
        get
        {
            var result=attributes;
            foreach(var gear in equipped)
            {
                if(gear==null)continue;var bonus=gear.AttributeBonus;
                result=new(result.Strength+bonus.Strength,result.Dexterity+bonus.Dexterity,result.Constitution+bonus.Constitution,result.Intelligence+bonus.Intelligence);
            }
            return result;
        }
    }
    string GearStats(Gear gear)
    {
        if(gear.Slot==GearSlot.Weapon)return T($"Dano: {new DamageDice(1,gear.Sides,gear.Power)} + ",$"Damage: {new DamageDice(1,gear.Sides,gear.Power)} + ")+(gear.Kind==GearKind.Sword?T("FOR","STR"):gear.Kind==GearKind.Staff?"INT":T("DES","DEX"));
        var b=gear.AttributeBonus;var parts=new List<string>();
        if(gear.Slot==GearSlot.Armor)parts.Add($"{T("CA","AC")} {gear.ArmorClass}");
        if(b.Strength!=0)parts.Add(T($"FOR +{b.Strength}",$"STR +{b.Strength}"));
        if(b.Dexterity!=0)parts.Add(T($"DES +{b.Dexterity}",$"DEX +{b.Dexterity}"));
        if(b.Constitution!=0)parts.Add($"CON +{b.Constitution}");
        if(b.Intelligence!=0)parts.Add($"INT +{b.Intelligence}");
        return string.Join(" | ",parts);
    }
    int WeaponBonus=>Weapon?.Power??0;
    int DamageReduction=>equipped[1]?.Quality switch {Rarity.Epic=>1,Rarity.Legendary=>2,_=>0};
    string GearName(Gear g)=>T(new[]{"Espada","Adaga","Cajado","Arco","Placas","Couro","Manto","Amuleto"}[(int)g.Kind],new[]{"Sword","Dagger","Staff","Bow","Plate","Leather","Robe","Amulet"}[(int)g.Kind]);
    string RarityName(Rarity r)=>T(new[]{"Comum","Raro","Epico","Lendario"}[(int)r],new[]{"Common","Rare","Epic","Legendary"}[(int)r]);
    Color RarityColor(Rarity r)=>r switch {Rarity.Rare=>new Color("69b7ff"),Rarity.Epic=>new Color("cd8cff"),Rarity.Legendary=>gold,_=>ink};
    string GearLabel(Gear g)=>$"{GearName(g)} / {RarityName(g.Quality)}"+(g.Grade>0?$" +{g.Grade}":"");
    string SlotName(int slot)=>T(new[]{"ARMA","ARMADURA","ACESSORIO"}[slot],new[]{"WEAPON","ARMOR","ACCESSORY"}[slot]);
    void ResetInventory()
    {
        backpack.Clear();Array.Clear(equipped);potions=5;energyPotions=inventoryIndex=0;inventoryNotice=("","");
        var starter=new Gear(new[]{GearKind.Sword,GearKind.Staff,GearKind.Bow,GearKind.Dagger}[selected],Rarity.Common);
        backpack.Add(starter);equipped[0]=starter;
    }
    static int CycleIndex(int depth)=>Math.Max(0,(depth-1)/5);
    static double ChestChance(int depth)=>Math.Min(.60,.42+.01*CycleIndex(depth));
    static double ExtraChestDropChance(int depth)=>Math.Min(.35,.03*CycleIndex(depth));
    Rarity RollRarity(int depth,bool guardian=false)
    {
        int cycle=CycleIndex(depth),roll=rng.Next(100);
        int legendary=guardian?Math.Min(35,2+cycle*3):Math.Min(15,cycle);
        int epic=guardian?Math.Min(45,10+cycle*3):Math.Min(25,cycle*2);
        int rare=Math.Min(40,15+cycle*2);
        return roll<legendary?Rarity.Legendary:roll<legendary+epic?Rarity.Epic:guardian||roll<legendary+epic+rare?Rarity.Rare:Rarity.Common;
    }
    Gear DropEquipment(int depth,bool guardian=false)
    {
        var gear=new Gear((GearKind)rng.Next(8),RollRarity(depth,guardian),(depth-1)/20);
        backpack.Add(gear);return gear;
    }
    void OpenChest()
    {
        int drops=1+(rng.NextDouble()<ExtraChestDropChance(floor)?1:0);
        for(int i=0;i<drops;i++)
        {
            if(rng.NextDouble()<.70)
            {
                var gear=DropEquipment(floor);
                Say($"Bau: {GearNameFor(gear,false)}. [I] inventario.",$"Chest: {GearNameFor(gear,true)}. [I] inventory.");
            }
            else if(rng.Next(2)==0){potions++;Say("Bau: +1 pocao de vida.","Chest: +1 health potion.");}
            else {energyPotions++;Say("Bau: +1 pocao de energia.","Chest: +1 energy potion.");}
        }
    }
    int RollEnemyGold(char glyph,int depth)
    {
        int cycle=CycleIndex(depth);
        if(glyph=='B')return rng.Next(18+cycle*8,29+cycle*8);
        return rng.Next(2)==0?0:rng.Next(1+cycle,3+cycle*2);
    }
    string GearNameFor(Gear gear,bool en)
    {
        bool previous=english;english=en;string name=GearLabel(gear);english=previous;return name;
    }
    void InventoryMessage(string pt,string en){inventoryNotice=(pt,en);}
    void ToggleGear(Gear gear)
    {
        int slot=(int)gear.Slot;
        if(!backpack.Any(g=>ReferenceEquals(g,gear)))return;
        if(ReferenceEquals(equipped[slot],gear))equipped[slot]=null;
        else
        {
            if(!gear.Allows(selected)){InventoryMessage("Sua classe nao pode usar este item.","Your class cannot use this item.");return;}
            if(level<gear.RequiredLevel){InventoryMessage($"Requer nivel {gear.RequiredLevel}.",$"Requires level {gear.RequiredLevel}.");return;}
            equipped[slot]=gear;
        }
        aiming=false;
        Say($"Equipamento alterado: {GearNameFor(gear,false)}.",$"Equipment changed: {GearNameFor(gear,true)}.");
        screen="game";EndTurn();
    }
    void DrinkEnergy()
    {
        if(energyPotions==0||energy==maxEnergy){InventoryMessage("Sem pocao ou energia ja cheia.","No potion or energy already full.");return;}
        int roll=rng.Next(1,7)+rng.Next(1,7),healing=Math.Min(maxEnergy-energy,roll);
        energy+=healing;energyPotions--;screen="game";
        Say($"Pocao de energia: 2d6 = {roll}. +{healing} EN.",$"Energy potion: 2d6 = {roll}. +{healing} EN.");EndTurn();
    }
    void HandleInventory(Key key)
    {
        int count=5+backpack.Count;
        if(Previous(key))inventoryIndex=(inventoryIndex+count-1)%count;
        if(Next(key))inventoryIndex=(inventoryIndex+1)%count;
        if(!Confirm(key))return;
        if(inventoryIndex<3)
        {
            if(equipped[inventoryIndex] is Gear g)ToggleGear(g);
            else InventoryMessage("Slot vazio. Selecione um item na mochila.","Empty slot. Select an item in the backpack.");
        }
        else if(inventoryIndex==3)
        {
            if(potions==0||hp==maxHp){InventoryMessage("Sem pocao ou vida ja cheia.","No potion or health already full.");return;}
            screen="game";Drink();
        }
        else if(inventoryIndex==4)DrinkEnergy();
        else ToggleGear(backpack[inventoryIndex-5]);
    }
    string GearEffect(Gear g)=>g.Quality<Rarity.Epic?T("Sem efeito especial.","No special effect."):g.Slot switch
    {
        GearSlot.Weapon=>g.Quality==Rarity.Epic?T("Impacto: +1d4 de dano ao acertar.","Impact: +1d4 damage on hit."):T("Impacto: +1d6; drena ate 2 PV ao acertar.","Impact: +1d6; drains up to 2 HP on hit."),
        GearSlot.Armor=>T($"Protecao: reduz dano recebido em {(g.Quality==Rarity.Epic?1:2)}.",$"Protection: reduces incoming damage by {(g.Quality==Rarity.Epic?1:2)}."),
        _=>T($"Foco: +{(g.Quality==Rarity.Epic?1:2)} energia por abate.",$"Focus: +{(g.Quality==Rarity.Epic?1:2)} energy per kill.")
    };
    void DrawInventory()
    {
        Text(32,202,T("EQUIPADO","EQUIPPED"),gold,16);
        for(int i=0;i<3;i++)
        {
            var g=equipped[i];Text(32,234+i*31,(inventoryIndex==i?"> ":"  ")+SlotName(i)+": "+(g==null?T("Vazio","Empty"):GearLabel(g)),g==null?dim:RarityColor(g.Quality),15);
        }
        Text(32,351,T("CONSUMIVEIS","CONSUMABLES"),gold,16);
        Text(32,380,(inventoryIndex==3?"> ":"  ")+T($"Pocao de vida x{potions}",$"Health potion x{potions}"),inventoryIndex==3?teal:ink,16);
        Text(32,410,(inventoryIndex==4?"> ":"  ")+T($"Pocao de energia x{energyPotions}",$"Energy potion x{energyPotions}"),inventoryIndex==4?teal:ink,16);
        int page=Math.Max(0,inventoryIndex-5)/6;
        Text(32,450,T($"MOCHILA / PAGINA {page+1}",$"BACKPACK / PAGE {page+1}"),gold,16);
        for(int i=page*6;i<Math.Min(backpack.Count,page*6+6);i++)
        {
            var g=backpack[i];bool worn=equipped.Any(e=>ReferenceEquals(e,g));
            Text(32,482+(i-page*6)*29,(inventoryIndex==i+5?"> ":"  ")+(worn?"[*] ":"[ ] ")+GearLabel(g),RarityColor(g.Quality),16);
        }
        Frame(570,198,72,26,dim,15,18);
        Gear? detail=inventoryIndex<3?equipped[inventoryIndex]:inventoryIndex>=5?backpack[inventoryIndex-5]:null;
        if(detail is Gear item)
        {
            Text(590,232,GearLabel(item),RarityColor(item.Quality),21);
            Text(590,270,$"{SlotName((int)item.Slot)} / {T("VALOR","VALUE")} {item.Value} {T("OURO","GOLD")}",ink,16);
            Text(590,308,T($"Requer nivel {item.RequiredLevel}",$"Requires level {item.RequiredLevel}"),level>=item.RequiredLevel?teal:red,17);
            string classes=string.Join(" / ",Enumerable.Range(0,4).Where(item.Allows).Select(ClassName));
            Text(590,340,classes, item.Allows(selected)?teal:red,14);
            string stats=GearStats(item);
            Text(590,388,stats,ink,16);
            Text(590,438,T("EFEITO ESPECIAL","SPECIAL EFFECT"),gold,16);
            Text(590,471,GearEffect(item),ink,15);
            Text(590,526,T("[ENTER] equipar / remover","[ENTER] equip / remove"),teal,17);
        }
        else if(inventoryIndex is 3 or 4)
        {
            bool life=inventoryIndex==3;
            Text(590,240,life?T("POCAO DE VIDA","HEALTH POTION"):T("POCAO DE ENERGIA","ENERGY POTION"),teal,22);
            Lines(590,300,life?T("Cura 2d10 PV (2-20).\nValor: 12 ouro.\nQualquer classe; nivel 1.","Heals 2d10 HP (2-20).\nValue: 12 gold.\nAny class; level 1."):T("Restaura 2d6 energia (2-12).\nValor: 15 ouro.\nQualquer classe; nivel 1.","Restores 2d6 energy (2-12).\nValue: 15 gold.\nAny class; level 1."),ink,18,34);
            Text(590,450,T("[ENTER] beber","[ENTER] drink"),teal,18);
        }
        else Text(590,240,T("Selecione um item na mochila.","Select an item in the backpack."),ink,18);
        Text(590,574,T("Equipar ou beber gasta um turno","Equipping or drinking uses one turn"),dim,16);
        Text(590,601,T("e retoma a partida.","and resumes the game."),dim,16);
        Text(32,692,T(inventoryNotice.Pt,inventoryNotice.En),red,16);
    }
}
