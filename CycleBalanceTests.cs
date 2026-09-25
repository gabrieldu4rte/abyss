using Godot;
using System;
using System.Linq;
using static TabletopRules;

public partial class Main
{
    void TestCycleBalance()
    {
        selected=0;Start(77);enemies.Clear();attributes=new Attributes(20,12,14,8);
        equipped[0]=new Gear(GearKind.Sword,Rarity.Common);
        if(MeleeModifier!=5||MeleeBonus!=Proficiency+5||MeleeDice.Bonus!=5)throw new Exception("Sword did not use STR for accuracy and damage");
        equipped[0]=new Gear(GearKind.Dagger,Rarity.Common);
        if(MeleeModifier!=1||MeleeDice.Bonus!=1||PrimaryModifier!=5)throw new Exception("Finesse weapon did not use DEX independently of warrior ability");
        selected=3;equipped[0]=new Gear(GearKind.Sword,Rarity.Common);
        if(MeleeModifier!=5||PrimaryModifier!=1||AbilityDice.Bonus!=1)throw new Exception("Rogue sword and skill attributes conflated");
        selected=1;equipped[0]=new Gear(GearKind.Staff,Rarity.Common);
        if(SpellBonus!=Proficiency-1||ShotDice.Bonus!=-1||AbilityDice.Bonus!=-1)throw new Exception("Mage INT accuracy/damage failed");
        selected=2;equipped[0]=new Gear(GearKind.Bow,Rarity.Common);
        if(SpellBonus!=Proficiency+1||ShotDice.Bonus!=1||AbilityDice.Bonus!=1)throw new Exception("Archer DEX accuracy/damage failed");
        var foe=new Enemy(player+Vector2I.Right,'g',1);
        foreach(int hero in Enumerable.Range(0,4))
        {
            selected=hero;
            if(TargetDefense(foe,false,false)!=foe.Armor||TargetDefense(foe,true,false)!=foe.Armor||TargetDefense(foe,false,true)!=foe.Armor)throw new Exception("Physical and magic attacks use different defenses");
        }
        selected=0;equipped[0]=null;
        if(Defense!=12)throw new Exception("Unarmored defense is not 10 + CON modifier");
        int saved=Defense;attributes=attributes with{Dexterity=30,Intelligence=30};
        if(Defense!=saved)throw new Exception("DEX or INT still changes defense");
        foreach(var kind in new[]{GearKind.Plate,GearKind.Leather,GearKind.Robe})
        {
            var armor=new Gear(kind,Rarity.Rare);equipped[1]=armor;
            if(Defense!=armor.ArmorClass+EffectiveAttributes.Con)throw new Exception("Armor class + CON formula failed");
        }
        equipped[1]=null;var baseStats=attributes;
        equipped[2]=new Gear(GearKind.Amulet,Rarity.Rare);
        if(EffectiveAttributes.Strength!=baseStats.Strength+2||EffectiveAttributes.Constitution!=baseStats.Constitution+2)throw new Exception("Accessory attribute bonus missing");
        equipped[2]=null;if(EffectiveAttributes!=baseStats)throw new Exception("Removed equipment left attribute bonuses");
        foreach(char glyph in new[]{'r','s','g','B'})
        {
            var e=new Enemy(Vector2I.Zero,glyph,1);int attack=e.AttackBonus,damage=e.Dice.Bonus,def=e.Armor;
            e.Stats=glyph is 'r' or 'g'?e.Stats with{Dexterity=e.Stats.Dexterity+2}:e.Stats with{Strength=e.Stats.Strength+2};
            if(e.AttackBonus!=attack+1||e.Dice.Bonus!=damage+1||e.Armor!=def)throw new Exception("Enemy attack attribute does not affect both accuracy and damage");
            e.Stats=e.Stats with{Constitution=e.Stats.Constitution+2};if(e.Armor!=def+1)throw new Exception("Enemy CON defense failed");
        }
        if(ChestChance(1)<=.35||ChestChance(6)<=ChestChance(5)||ExtraChestDropChance(6)<=ExtraChestDropChance(1))throw new Exception("Chest cycle increase missing");
        for(int depth=1;depth<=100;depth++)
        {
            if(ChestChance(depth)>.60||ExtraChestDropChance(depth)>.35)throw new Exception("Chest rewards exceed caps");
            for(int roll=0;roll<100;roll++)
            {
                // Same roll can only maintain or improve rarity across cycles.
                rng=new RangeRandom(roll);var early=RollRarity(depth);
                rng=new RangeRandom(roll);var later=RollRarity(depth+5);
                if(later<early)throw new Exception("Chest rarity regressed between cycles");
                rng=new RangeRandom(roll);early=RollRarity(depth,true);
                rng=new RangeRandom(roll);later=RollRarity(depth+5,true);
                if(early<Rarity.Rare||later<early)throw new Exception("Guardian rarity regressed or fell below rare");
            }
        }
        foreach(int depth in new[]{5,10,25,100})
        {
            selected=0;Start(depth);enemies.Clear();floor=depth;rng=new Random(depth);
            var boss=new Enemy(player+Vector2I.Right,'B',depth);enemies.Add(boss);
            int money=coins,count=backpack.Count;Hit(boss,boss.Hp);
            int cycle=CycleIndex(depth),reward=coins-money;
            if(reward<18+cycle*8||reward>28+cycle*8||backpack.Count!=count+1||backpack[^1].Quality<Rarity.Rare)throw new Exception("Guardian guaranteed reward failed");
            for(int i=0;i<500;i++)
            {
                int gold=RollEnemyGold('r',depth);
                if(gold!=0&&(gold<1+cycle||gold>2+cycle*2))throw new Exception("Normal enemy gold outside cycle range");
            }
        }
        GD.Print("CYCLE AUDIT: weapon/class attributes, unified CON defense, reversible gear bonuses, rarity progression, chest caps and guaranteed guardian loot passed.");
    }
    sealed class RangeRandom : Random
    {
        readonly int value;
        public RangeRandom(int value){this.value=value;}
        public override int Next(int maxValue)=>Math.Clamp(value,0,maxValue-1);
    }
}
