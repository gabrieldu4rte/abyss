using Godot;
using System;
using System.Linq;

public partial class Main
{
    void TestInventory()
    {
        for(int hero=0;hero<4;hero++)
        {
            selected=hero;Start(321);enemies.Clear();
            if(backpack.Count!=1||Weapon?.Quality!=Rarity.Common||equipped[1]!=null||equipped[2]!=null||potions!=5||energyPotions!=0)throw new Exception("Starting inventory incorrect");
            var weapon=Weapon!;double armed=MeleeDice.Average;ToggleGear(weapon);
            if(Weapon!=null||MeleeDice.Average>=armed||CanShoot)throw new Exception("Unarmed damage or ranged restriction failed");
            ToggleGear(weapon);
            if(hero is 1 or 2)
            {
                energy=0;int before=turn;Shoot(Vector2I.Right);
                if(energy!=0||turn!=before+1||ShotDice.Sides!=weapon.Sides)throw new Exception("Weapon-based free ranged attack failed");
                var delta=Directions.First(d=>Walk(player+d));
                var rangedTarget=new Enemy(player+delta,'r',1){Hp=999,MaxHp=999};enemies.Add(rangedTarget);
                rng=new FixedRandom(20);before=turn;Shoot(delta);
                if(rangedTarget.Hp!=999-(weapon.Sides*2+PrimaryModifier)||energy!=0||turn!=before+1)throw new Exception("Ranged weapon damage did not reach target");
                enemies.Clear();
            }
            foreach(var kind in Enum.GetValues<GearKind>())
            {
                var gear=new Gear(kind,Rarity.Common);backpack.Add(gear);
                int before=turn;ToggleGear(gear);
                if(gear.Allows(hero)?!ReferenceEquals(equipped[(int)gear.Slot],gear):turn!=before)throw new Exception("Class equipment restriction failed");
            }
        }
        selected=0;Start(77);enemies.Clear();
        var legendary=new Gear(GearKind.Sword,Rarity.Legendary);backpack.Add(legendary);
        int oldTurn=turn;ToggleGear(legendary);
        if(turn!=oldTurn||ReferenceEquals(Weapon,legendary))throw new Exception("Level requirement bypassed");
        level=10;ToggleGear(legendary);hp=maxHp-10;
        var enemy=new Enemy(player+Vector2I.Right,'r',1){Hp=999,MaxHp=999};rng=new FixedRandom(20);
        int oldHp=hp;ResolveHeroAttack(enemy);
        if(hp!=oldHp+2||enemy.Hp!=999-(legendary.Sides*2+attributes.Str+legendary.Power+6))throw new Exception("Legendary impact/drain failed");
        var epic=new Gear(GearKind.Sword,Rarity.Epic);backpack.Add(epic);ToggleGear(epic);
        oldHp=hp;int enemyHp=enemy.Hp;ResolveHeroAttack(enemy);
        if(hp!=oldHp||enemy.Hp!=enemyHp-(epic.Sides*2+attributes.Str+epic.Power+4))throw new Exception("Epic impact failed");
        int oldDefense=Defense;var armor=new Gear(GearKind.Plate,Rarity.Legendary);backpack.Add(armor);ToggleGear(armor);
        if(Defense<=oldDefense||DamageReduction!=2)throw new Exception("Armor bonuses failed");
        hp=maxHp;int full=hp;ResolveEnemyAttack(enemy,false);
        if(hp!=full-Math.Max(1,TabletopRules.RollDamage(new FixedRandom(20),enemy.Dice,true)-2))throw new Exception("Armor reduction not applied to actual damage");
        ToggleGear(armor);if(Defense!=oldDefense||DamageReduction!=0)throw new Exception("Removed armor retained bonuses");
        var charm=new Gear(GearKind.Amulet,Rarity.Epic);backpack.Add(charm);ToggleGear(charm);energy=0;
        var victim=new Enemy(player+Vector2I.Right,'r',1){Hp=1};enemies.Add(victim);Hit(victim,2);
        if(energy!=1||EffectiveAttributes.Strength!=attributes.Strength+3)throw new Exception("Accessory effects failed");
        rng=new Random(123);for(int i=0;i<1000;i++)if(RollRarity(1)>Rarity.Rare)throw new Exception("Early rarity gate failed");
        var qualities=Enumerable.Range(0,2000).Select(_=>RollRarity(100)).Distinct().Count();
        if(qualities!=4)throw new Exception("Deep rarity pool incomplete");
        foreach(var kind in Enum.GetValues<GearKind>())
        {
            int value=0,power=-1;
            foreach(var quality in Enum.GetValues<Rarity>())
            {
                var gear=new Gear(kind,quality);
                if(gear.Value<=value||gear.Power<=power)throw new Exception("Rarity scaling failed");
                value=gear.Value;power=gear.Power;
            }
        }
        Start(77);enemies.Clear();items.Clear();items[player]='C';int beforeCount=backpack.Count;
        rng=new FixedRandom(1);Pickup();int after=backpack.Count;Pickup();
        if(after!=beforeCount+1||backpack.Count!=after||items.ContainsKey(player))throw new Exception("Chest opened twice or lost loot");
        energy=0;energyPotions=1;rng=new FixedRandom(1);DrinkEnergy();
        if(energyPotions!=0||energy!=2)throw new Exception("Energy potion failed");
        screen="game";HandleKey(Key.I);if(screen!="pause"||pauseTab!=1)throw new Exception("Inventory shortcut failed");
        for(int i=0;i<20;i++)backpack.Add(new Gear(GearKind.Dagger,Rarity.Common));
        for(int i=0;i<backpack.Count+5;i++)HandleKey(Key.Down);
        if(inventoryIndex!=0)throw new Exception("Inventory navigation wrap failed");
        GD.Print("INVENTORY AUDIT: starting gear, slots, class/level restrictions, unarmed/ranged damage, rarity effects, chest uniqueness, consumables and navigation passed.");
    }
}
