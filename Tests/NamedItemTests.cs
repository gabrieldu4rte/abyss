using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
namespace Abyss.Tests;
internal sealed partial class RegressionSuite
{
    internal void TestNamedItems()
    {
        var g = new GameSession(new TestHost(), new TestCanvas(), new TestSettings());
        var d = g.DungeonState; var p = g.PlayerState; var inv = g.InventoryState; var world = d.Environment;
        void Check(bool condition, string why) { if (!condition) throw new Exception(why); }
        void Reset(int hero = 0)
        {
            p.ClassIndex = hero; g.Start(734); d.Enemies.Clear(); d.Items.Clear(); world.Clear(); d.Modifier = FloorModifier.None;
            for (int x=1;x<GameRules.Width-1;x++) for(int y=1;y<GameRules.Height-1;y++) { d.Tiles[x,y]='.'; d.Visible[x,y]=d.Explored[x,y]=true; }
            p.Position = new(20,13); p.Health=p.MaxHealth=1000; p.Energy=p.MaxEnergy=100; p.Level=20;
            d.StairsRoom=new Rect2I(40,8,10,8); d.Stairs=new(45,12); d.Tiles[45,12]='>';
            g.RandomGenerator = new ItemRandom(20);
        }
        Gear Equip(ItemId id)
        {
            var gear=NamedItemCatalog.Create(id); inv.Backpack.Add(gear); inv.Equipped[(int)gear.Slot]=gear; return gear;
        }
        Enemy EnemyAt(int distance=1, char glyph='g', int depth=1)
        {
            var e = new Enemy(p.Position+Vector2I.Right*distance,glyph,depth) { Health=200,MaxHealth=200 }; d.Enemies.Add(e); return e;
        }
        Reset(); Equip(ItemId.SparkSword); var enemy=EnemyAt(); world.Oil.Add(enemy.Position);
        g.RandomGenerator=new ItemRandom(20,0); g.CombatService.ResolveHeroAttack(enemy);
        Check(world.Fire.ContainsKey(enemy.Position), "Spark Sword did not ignite oil on hit.");
        Reset(); Equip(ItemId.SparkSword); enemy=EnemyAt(); world.Oil.Add(enemy.Position);
        g.RandomGenerator=new ItemRandom(20,.99); g.CombatService.ResolveHeroAttack(enemy);
        Check(world.Fire.Count==0, "Spark Sword ignored its chance.");
        foreach(bool fungal in new[]{false,true})
        {
            Reset(2); Equip(ItemId.VenomBow); enemy=EnemyAt(3,fungal?'m':'g',fungal?11:1);
            g.CombatService.ResolveHeroAttack(enemy,true);
            Check(world.PoisonedEnemies.ContainsKey(enemy)!=fungal,"Venom critical or fungal immunity failed.");
            if (!fungal) { int hp=enemy.Health; for(int i=0;i<3;i++) g.EnvironmentService.Tick(); Check(enemy.Health==hp-6 && !world.PoisonedEnemies.ContainsKey(enemy),"Venom duration/damage failed."); }
        }
        Reset(); Equip(ItemId.InsulatingLeather); enemy=EnemyAt(); world.Fixtures[enemy.Position]=Fixture.ShockTrap;
        g.EnvironmentService.Tick(); Check(p.Health==1000,"Insulating armor did not stop shock splash.");
        world.Fixtures[p.Position]=Fixture.ShockTrap; g.EnvironmentService.Tick(); Check(p.Health==997,"Insulating armor wrongly stopped direct shock.");
        world.Fixtures.Clear(); world.Fire[p.Position]=4; int previous=p.Health; g.EnvironmentService.Tick(); Check(p.Health==previous-2,"Insulating fire reduction failed.");
        Reset(); var ring=Equip(ItemId.CampRing);
        Check(inv.TorchFuel==120 && g.MerchantService.TradePrice(new Offer { Potion=2 })==5,"Camp ring fuel/price failed.");
        for(int i=0;i<60;i++) g.EnvironmentService.Tick(); Check(inv.TorchFuel==60,"Camp torch did not burn at 120-turn pace.");
        inv.Equipped[2]=null; Check(inv.TorchFuel==50,"Removing ring reset the active torch.");
        inv.Equipped[2]=ring; Check(inv.TorchFuel==60,"Reequipping ring refilled the torch.");
        for(int i=0;i<60;i++)g.EnvironmentService.Tick(); Check(!inv.HasLight,"Camp torch did not expire at 120 turns.");
        inv.SpareTorches=1; inv.LightReserve(); Check(inv.TorchFuel==120,"Replacement torch lost camp duration.");
        Reset(1); Equip(ItemId.FluidStaff); enemy=EnemyAt(3); var wet=p.Position+Vector2I.Right; world.Details[wet]=':'; world.Fire[wet]=4;
        var permanent=p.Position+Vector2I.Right*2; world.Details[permanent]='~'; g.PlayerActions.Shoot(Vector2I.Right);
        Check(!world.Fire.ContainsKey(wet) && world.Details[wet]=='~' && world.TemporaryWater[wet].Turns==1,"Fluid staff failed to create its first turn of water.");
        g.EnvironmentService.Tick(); Check(world.Details[wet]==':' && world.Details[permanent]=='~' && !world.TemporaryWater.ContainsKey(wet),"Water expired incorrectly or erased natural water.");
        Reset(); Equip(ItemId.ThrowingGauntlets); enemy=EnemyAt(2); var second=EnemyAt(5); d.Tiles[28,13]='#';
        g.EnvironmentService.ThrowTorch(Vector2I.Right);
        Check(enemy.Health==199 && second.Health==199 && enemy.Position==new Vector2I(23,13) && second.Position==new Vector2I(26,13),"Piercing torch did not hit/push each target exactly once.");
        Check(g.VisualEffects.Actions.Animations.Last().Path.Last()==new Vector2I(27,13),"Piercing torch did not stop at wall.");
        Reset(); Equip(ItemId.DeepBreathMantle); d.Modifier=FloorModifier.ThinAir; p.Energy=0;
        for(int i=0;i<6;i++)g.EndTurn(); Check(p.Energy==1,"Mantle did not restore normal energy regeneration.");
        inv.EnergyPotions=1; p.Energy=0; g.RandomGenerator=new ItemSequenceRandom(1,4,6); g.InventoryService.DrinkEnergy();
        Check(p.Energy==10,"Energy advantage did not discard the lowest d6.");
        Reset(3); Equip(ItemId.ExecutionerBlade); enemy=EnemyAt(); g.RandomGenerator=new ItemRandom(10);
        int expected=TabletopRules.RollDamage(new ItemRandom(10),g.HeroCombatStats.MeleeDice,false)+6;
        g.CombatService.ResolveHeroAttack(enemy); Check(p.Health==999 && enemy.Health==200-expected,"Executioner sacrifice/bonus failed.");
        Reset(3); Equip(ItemId.ExecutionerBlade); enemy=EnemyAt(); p.Health=1; g.CombatService.ResolveHeroAttack(enemy);
        Check(p.Health==0 && enemy.Health==200 && g.RunState.Screen=="dead","Lethal sacrifice still executed an attack.");
        Reset(); Equip(ItemId.SpellforgeBlade); enemy=EnemyAt(); second=EnemyAt(2); previous=second.Health;
        g.CombatService.ResolveHeroAttack(enemy); Check(second.Health<previous && second.Position==new Vector2I(23,13),"Spellforge critical failed to damage and push nearby enemy.");
        Check(p.Energy==100 && g.VisualEffects.Actions.Animations.Any(a=>a.Kind==ActionAnimationKind.SeismicImpact),"Seismic passive used energy or lacked animation.");
        Reset(2); Equip(ItemId.RevengeBow); enemy=EnemyAt(7); enemy.PromoteElite(EliteTitle.Cruel); previous=enemy.Health;
        g.RandomGenerator=new ItemSequenceRandom(1,20,1,1); g.CombatService.ResolveHeroAttack(enemy,true);
        Check(enemy.Health<previous && g.ExpeditionJournal.LastRollEn.Contains("[1,20]"),"Revenge advantage did not select higher roll.");
        Check(g.CombatService.AttackBonus(enemy,true,false)==g.HeroCombatStats.SpellBonus,"Revenge bow still penalizes range.");
        Reset(); Equip(ItemId.SandflowerRelic); enemy=EnemyAt(2); var wall=p.Position+Vector2I.Up; d.Tiles[wall.X,wall.Y]='#'; d.Explored[wall.X,wall.Y]=false;
        p.Health=900; g.HeroVitals.Heal(1);
        Check(enemy.BlindTurns==5 && d.Explored[wall.X,wall.Y],"Sandflower did not blind/reveal walls.");
        var before=enemy.Position; for(int i=0;i<5;i++)g.EnemyAi.ActEnemy(enemy); Check(enemy.Position==before && enemy.BlindTurns==0,"Blind did not skip exactly five responses.");
        g.HeroVitals.Heal(1,false); Check(enemy.BlindTurns==0,"Floor healing activated Sandflower.");
        p.Health=p.MaxHealth; g.HeroVitals.Heal(1); Check(enemy.BlindTurns==0,"Zero healing activated Sandflower.");
        foreach(bool lit in new[]{false,true})
        {
            Reset(); var hood=Equip(ItemId.ShadowLegendsHood); inv.TorchEquipped=lit; p.Health=1; g.HeroVitals.Damage(10);
            if(lit) Check(p.Health==0 && g.RunState.Screen=="dead" && inv.Backpack.Contains(hood),"Hood activated with torch lit.");
            else
            {
                Check(p.Health==1 && p.Energy==0 && d.StairsRoom.HasPoint(p.Position) && inv.Equipped[1]==null && !inv.Backpack.Contains(hood),"Hood rescue/break failed.");
                Check(g.VisualEffects.Actions.Animations.Any(a=>a.Kind==ActionAnimationKind.ShadowBirds),"Shadow escape has no bird animation.");
                g.HeroVitals.Damage(1); Check(p.Health==0,"Broken hood activated twice.");
            }
        }
        Reset(); Equip(ItemId.ShadowLegendsHood); inv.TorchEquipped=false; p.Health=1; world.Fire[p.Position]=3; world.HeroPoisonTurns=3;
        g.EndTurn(); Check(p.Health==1 && p.Energy==0 && g.RunState.Screen=="game","Rescue did not interrupt the environmental damage chain.");
        Reset(); g.RandomGenerator=new Random(773); var seen=new HashSet<ItemId>(); var legends=new HashSet<ItemId>();
        for(int i=0;i<4000;i++)
        {
            var gear=g.LootService.CreateEquipment(101,true);
            if(gear.Special==ItemId.None)continue;
            seen.Add(gear.Special);
            Check(gear.Quality==NamedItemCatalog.Create(gear.Special).Quality,"Named item rarity changed.");
            if(gear.Quality==Rarity.Legendary)Check(legends.Add(gear.Special),"Legendary unique repeated in one expedition.");
            Check(NamedItemText.Name(gear.Special,false).Length>0 && NamedItemText.Effect(gear.Special,true).Length>0,"Missing localization.");
        }
        Check(seen.Count==12,"Not all named items are obtainable.");
        GD.Print("NAMED ITEM AUDIT: all twelve effects, acquisition, uniqueness, torch lifetime, immunity, temporary water, piercing, advantage, sacrifice, blindness and lethal rescue passed.");
    }
    private sealed class ItemRandom(int value, double chance=.5) : Random(811)
    {
        public override int Next(int min,int max)=>Math.Clamp(value,min,max-1);
        public override double NextDouble()=>chance;
    }
    private sealed class ItemSequenceRandom(params int[] values) : Random(811)
    {
        private int index;
        public override int Next(int min,int max)=>Math.Clamp(values[Math.Min(index++,values.Length-1)],min,max-1);
    }
}
