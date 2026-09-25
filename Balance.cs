using Godot;
using System;
using System.Linq;
using static TabletopRules;

public partial class Main
{
    Attributes attributes;
    int lastPotionRoll,lastPotionHealing;
    string lastRollPt="",lastRollEn="";
    bool CanMelee=>Weapon==null||selected is 0 or 3;
    bool CanShoot=>(selected==1&&Weapon?.Kind==GearKind.Staff)||(selected==2&&Weapon?.Kind==GearKind.Bow);
    int Proficiency=>2+(level-1)/4;
    int PrimaryModifier=>selected==0?EffectiveAttributes.Str:selected==1?EffectiveAttributes.Int:EffectiveAttributes.Dex;
    int MeleeModifier=>Weapon?.Kind switch {GearKind.Sword=>EffectiveAttributes.Str,GearKind.Dagger or GearKind.Bow=>EffectiveAttributes.Dex,GearKind.Staff=>EffectiveAttributes.Int,_=>PrimaryModifier};
    int Defense=>EquippedArmorClass+EffectiveAttributes.Con;
    int MeleeBonus=>Proficiency+MeleeModifier;
    int SpellBonus=>Proficiency+PrimaryModifier;
    int ShotCost=>0;
    int AbilityCost=>Math.Max(selected==1?4:3,new[]{7,10,8,8}[selected]-(level-1)/2);
    int XpToNext=>30+20*(level-1);
    DamageDice MeleeDice=>Weapon is Gear w?new(1,w.Sides,MeleeModifier+w.Power):new(1,2,0);
    DamageDice ShotDice=>Weapon is Gear w?new(1,w.Sides,PrimaryModifier+w.Power):new(1,2,0);
    DamageDice AbilityDice=>new(2,selected==2?8:6,PrimaryModifier+(level-1)/2+WeaponBonus);
    int AbilityRange=>new[]{2,5,10,3}[selected];
    static int EnemyXp(char glyph,int depth)=>glyph=='B'?18+depth*2:(glyph=='g'?4:glyph=='s'?3:2)+(depth-1)/2;
    static string MonsterName(char glyph,bool en)=>glyph switch {
        'r'=>en?"Crypt rat":"Rato das criptas",'s'=>en?"Skeleton":"Esqueleto",'g'=>"Goblin",'B'=>en?"Warden":"Guardiao",_=>en?"Enemy":"Inimigo"
    };
    void GainXp(int amount)
    {
        xp+=amount;
        while(xp>=XpToNext)
        {
            xp-=XpToNext;level++;
            if(level%2==0)
                attributes=selected==0?attributes with{Strength=attributes.Strength+1}:selected==1?attributes with{Intelligence=attributes.Intelligence+1}:attributes with{Dexterity=attributes.Dexterity+1};
            else attributes=attributes with{Constitution=attributes.Constitution+1};
            int growth=2+Math.Max(0,attributes.Con)/2;
            maxHp+=growth;hp=Math.Min(maxHp,hp+growth);
            if(level%2==0)maxEnergy++;
            energy=Math.Min(maxEnergy,energy+1);
            Say($"Nivel {level}: +1 atributo, +{growth} PV maximos.",$"Level {level}: +1 attribute, +{growth} max HP.");
        }
    }
    void BeginAim()
    {
        if(!CanShoot){aiming=false;Say("Equipe um cajado ou arco compativel para disparar.","Equip a compatible staff or bow to shoot.");return;}
        if(aiming){aiming=false;return;}
        
        aiming=true;Say($"Ataque basico: escolha a direcao. Sem custo de energia.",$"Basic attack: choose a direction. No energy cost.");
    }
    int AttackBonus(Enemy enemy,bool ranged,bool ability)
    {
        int bonus=(ranged||ability?SpellBonus:MeleeBonus)+(ability?2:0);
        if(ranged&&!ability&&Dist(player,enemy.P)>5)bonus-=2;
        return bonus;
    }
    int TargetDefense(Enemy enemy,bool ranged,bool ability)=>enemy.Armor;
    int ChanceAgainst(Enemy enemy,bool ranged,bool ability)=>HitChance(AttackBonus(enemy,ranged,ability),TargetDefense(enemy,ranged,ability),ability&&selected==3?19:20);
    void ResolveHeroAttack(Enemy enemy,bool ranged=false,bool ability=false)
    {
        if(!ranged&&!ability&&!CanMelee)return;
        if(!ranged&&!ability&&(Dist(player,enemy.P)!=1||!Los(player,enemy.P)))return;
        focus=enemy;focusHold=HurtDuration;
        var roll=ResolveAttack(rng.Next(1,21),AttackBonus(enemy,ranged,ability),TargetDefense(enemy,ranged,ability),ability&&selected==3?19:20);
        string total=$"d20({roll.Natural}){Signed(roll.Bonus)}={roll.Total} vs {roll.Defense}";
        lastRollPt=$"Voce: {total}";lastRollEn=$"You: {total}";
        if(!roll.Hit)
        {
            Say($"Voce -> {MonsterName(enemy.Glyph,false)}: {total}. Errou.",$"You -> {MonsterName(enemy.Glyph,true)}: {total}. You miss.");return;
        }
        var dice=ability?AbilityDice:ranged?ShotDice:MeleeDice;
        int damage=RollDamage(rng,dice,roll.Critical);
        if(Weapon is Gear weapon&&weapon.Quality>=Rarity.Epic)
        {
            int extra=rng.Next(1,weapon.Quality==Rarity.Legendary?7:5);damage+=extra;
            Say($"Impacto do equipamento: +{extra} dano.",$"Equipment impact: +{extra} damage.");
            if(weapon.Quality==Rarity.Legendary)hp=Math.Min(maxHp,hp+Math.Min(2,Math.Max(0,enemy.Hp)));
        }
        Say($"Voce -> {MonsterName(enemy.Glyph,false)}: {total}. {(roll.Critical?"CRITICO! ":"")}{damage} dano ({dice}).",
            $"You -> {MonsterName(enemy.Glyph,true)}: {total}. {(roll.Critical?"CRITICAL! ":"")}{damage} damage ({dice}).");
        Hit(enemy,damage,false);
    }
    void ResolveEnemyAttack(Enemy enemy,bool evade)
    {
        if(Dist(player,enemy.P)!=1||!Los(enemy.P,player))return;
        if(enemy.Glyph=='B'&&(!enemy.Alerted||!stairsRoom.HasPoint(player)))return;
        var roll=ResolveAttack(rng.Next(1,21),enemy.AttackBonus,Defense+(evade?4:0));
        string total=$"d20({roll.Natural}){Signed(roll.Bonus)}={roll.Total} vs {roll.Defense}";
        lastRollPt=$"{MonsterName(enemy.Glyph,false)}: {total}";lastRollEn=$"{MonsterName(enemy.Glyph,true)}: {total}";
        if(!roll.Hit){Say($"{MonsterName(enemy.Glyph,false)} -> voce: {total}. Errou.",$"{MonsterName(enemy.Glyph,true)} -> you: {total}. Missed.");return;}
        int damage=Math.Max(1,RollDamage(rng,enemy.Dice,roll.Critical)-DamageReduction);hp=Math.Max(0,hp-damage);HeroHurt(enemy,damage);
        Say($"{MonsterName(enemy.Glyph,false)} -> voce: {total}. {(roll.Critical?"CRITICO! ":"")}-{damage} PV.",$"{MonsterName(enemy.Glyph,true)} -> you: {total}. {(roll.Critical?"CRITICAL! ":"")}-{damage} HP.");
        if(hp==0)screen="dead";
    }
    static string Signed(int value)=>value>=0?$"+{value}":value.ToString();
    string StatsLine(Attributes a,bool first)=>first?T($"FOR {a.Strength}  DES {a.Dexterity}",$"STR {a.Strength}  DEX {a.Dexterity}"):T($"CON {a.Constitution}  INT {a.Intelligence}",$"CON {a.Constitution}  INT {a.Intelligence}");
}
