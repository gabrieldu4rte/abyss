using Godot;
using System;
using System.Linq;
namespace Abyss.Tests;
internal sealed partial class RegressionSuite
{
    internal void TestAdditionalNamedItems()
    {
        var g=new GameSession(new TestHost(),new TestCanvas(),new TestSettings());
        var p=g.PlayerState;var d=g.DungeonState;var w=d.Environment;var inv=g.InventoryState;
        void Check(bool c,string m){if(!c)throw new Exception(m);}
        void Reset(int hero=0)
        {
            p.ClassIndex=hero;g.Start(734);d.Enemies.Clear();d.Items.Clear();w.Clear();d.IsMerchantFloor=false;d.BlacksmithRoom=null;d.Modifier=FloorModifier.None;
            for(int x=1;x<GameRules.Width-1;x++)for(int y=1;y<GameRules.Height-1;y++){d.Tiles[x,y]='.';d.Visible[x,y]=true;}
            p.Position=new(20,13);d.Stairs=new(45,12);p.MaxHealth=p.Health=1000;p.MaxEnergy=100;p.Energy=0;p.Level=20;g.RandomGenerator=new ItemRandom(10);
        }
        void Equip(ItemId id){var gear=NamedItemCatalog.Create(id);inv.Equipped[(int)gear.Slot]=gear;}
        Enemy Target(int range=1){var e=new Enemy(p.Position+Vector2I.Right*range,'g',1){Health=200,MaxHealth=200};d.Enemies.Add(e);return e;}
        Reset();Equip(ItemId.ExplorerBlade);var e=Target();w.Details[e.Position]='~';g.RandomGenerator=new ItemSequenceRandom(1,15,1);
        int expected=TabletopRules.RollDamage(new ItemRandom(1),g.HeroCombatStats.MeleeDice,false)+2;
        g.CombatService.ResolveHeroAttack(e);Check(g.ExpeditionJournal.LastRollEn.Contains("[1,15]")&&e.Health==200-expected,"Explorer advantage/damage failed.");
        Reset();Equip(ItemId.VigorBracers);p.Health=900;g.PlayerActions.Drink();Check(p.Health==924&&g.ExpeditionJournal.LastPotionRoll==24,"Vigor potion bonus failed.");
        p.Health=999;g.PlayerActions.Drink();Check(p.Health==1000,"Vigor overhealed.");
        Reset();Equip(ItemId.ContinuousFlameBuckle);inv.TorchFuel=1;inv.SpareTorches=1;g.RandomGenerator=new ItemRandom(4);g.EnvironmentService.Tick();
        Check(p.Energy==4&&inv.TorchFuel==100&&inv.SpareTorches==0,"Automatic torch bonus failed.");
        inv.TorchFuel=1;g.EnvironmentService.Tick();Check(p.Energy==4,"Empty reserve restored energy.");
        inv.SpareTorches=1;inv.LightReserve();Check(p.Energy==4,"Manual reserve restored energy.");
        Reset();Equip(ItemId.InvestigatorMantle);w.Fixtures[p.Position]=Fixture.SpikeTrap;g.EnvironmentService.Tick();Check(p.Health==1000&&w.Fixtures[p.Position]==Fixture.SpikeTrap,"Investigator triggered spikes.");
        e=Target();e.Position=p.Position;p.Position+=Vector2I.Left;g.EnvironmentService.Tick();Check(e.Health==195&&w.Fixtures[e.Position]==Fixture.SpentTrap,"Protected spikes did not hurt enemy.");
        Reset(1);Equip(ItemId.EmberStaff);e=Target();w.Fire[e.Position]=4;g.CombatService.Hit(e,200);Check(p.Energy==2,"Burning kill did not restore energy.");
        e=Target();g.CombatService.Hit(e,200);Check(p.Energy==4,"Second burning kill did not restore energy.");
        w.Fire.Clear();e=Target();g.CombatService.Hit(e,200);Check(p.Energy==4,"Dry kill restored energy.");
        Reset(2);Equip(ItemId.TwilightBow);e=Target(6);expected=TabletopRules.RollDamage(new ItemRandom(10),g.HeroCombatStats.ShotDice,false)+3;g.CombatService.ResolveHeroAttack(e,true);Check(e.Health==200-expected,"Twilight distant damage failed.");
        e=Target(3);expected=TabletopRules.RollDamage(new ItemRandom(10),g.HeroCombatStats.ShotDice,false);g.CombatService.ResolveHeroAttack(e,true);Check(e.Health==200-expected,"Twilight bonus applied inside primary light.");
        Reset();Equip(ItemId.HunterGreedAmulet);e=Target();e.PromoteElite(EliteTitle.Cruel);g.RandomGenerator=new ItemRandom(10,.99);int count=inv.Backpack.Count;g.CombatService.Hit(e,9999);Check(inv.Backpack.Count==count+1&&inv.Backpack.Last().Quality>=Rarity.Rare,"Guaranteed elite drop failed.");
        Reset();Equip(ItemId.ThickRubberBoots);w.Fixtures[p.Position]=Fixture.ShockTrap;g.EnvironmentService.Tick();Check(p.Health==1000,"Rubber boots failed direct shock immunity.");
        g.EnvironmentService.Discharge(p.Position+Vector2I.Right);Check(p.Health==1000,"Rubber boots failed splash immunity.");
        Reset(2);Equip(ItemId.ConductiveCrossbow);e=Target(3);w.Details[e.Position]='~';var adjacent=Target(4);g.CombatService.ResolveHeroAttack(e,true);Check(adjacent.Health==197,"Crossbow did not discharge on water.");
        w.Details.Clear();adjacent.Health=200;g.CombatService.ResolveHeroAttack(e,true);Check(adjacent.Health==200,"Crossbow discharged on dry ground.");
        Reset();Equip(ItemId.FungalSovereignCrown);p.Health=900;w.HeroPoisonTurns=3;w.Fixtures[p.Position]=Fixture.PoisonTrap;g.EnvironmentService.Tick();Check(p.Health==920&&w.HeroPoisonTurns==0&&w.Fixtures[p.Position]==Fixture.SpentTrap,"Fungal healing/immunity failed.");
        Reset();Equip(ItemId.EternalForgeRobe);w.Fire[p.Position]=4;w.Fixtures[p.Position]=Fixture.FlameTrap;g.EnvironmentService.Tick();Check(p.Health==1000,"Eternal robe failed thermal immunity.");
        w.Clear();var oil=p.Position+Vector2I.Right*2;w.Oil.Add(oil);g.EnvironmentService.ThrowTorch(Vector2I.Right);Check(w.Fire[oil]==6,"Eternal torch oil did not last six turns.");
        foreach(var id in Enum.GetValues<ItemId>().Where(i=>i>=ItemId.ExplorerBlade))
        {
            var gear=NamedItemCatalog.Create(id);Check(NamedItemText.Name(id,true)!=""&&NamedItemText.Effect(id,false)!="","New item lacks text.");
            if(gear.Slot==GearSlot.Armor)for(int hero=0;hero<4;hero++)Check(gear.Allows(hero),"New armor is class-restricted.");
        }
        GD.Print("ADDITIONAL ITEMS AUDIT: eleven effects, dice, caps, automatic torch replacement, preserved traps, energy rewards, dim light, elite loot, discharge, poison conversion and six-turn oil fire passed.");
    }
}
