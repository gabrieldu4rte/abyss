using Godot;
using System;
using System.Linq;
namespace Abyss.Tests;
internal sealed partial class RegressionSuite
{
    internal void TestEnemyTraits()
    {
        var g=new GameSession(new TestHost(),new TestCanvas(),new TestSettings());
        var p=g.PlayerState;var d=g.DungeonState;var w=d.Environment;
        void Check(bool c,string m){if(!c)throw new Exception(m);}
        Enemy Setup(char glyph,int distance=1,int depth=1)
        {
            g.Start(734);d.Enemies.Clear();w.Clear();d.Items.Clear();d.IsMerchantFloor=false;d.BlacksmithRoom=null;
            for(int x=1;x<GameRules.Width-1;x++)for(int y=1;y<GameRules.Height-1;y++){d.Tiles[x,y]='.';d.Visible[x,y]=true;}
            p.Position=new(20,13);p.Health=p.MaxHealth=1000;p.Energy=p.MaxEnergy=100;
            var e=new Enemy(p.Position+Vector2I.Right*distance,glyph,depth);d.Enemies.Add(e);g.RandomGenerator=new ItemRandom(20,0);return e;
        }
        var e=Setup('s',4);var origin=e.Position;g.EnemyAi.ActEnemy(e);int health=p.Health;
        Check(health<1000&&e.Position==origin&&g.VisualEffects.Actions.Animations.Any(a=>a.Kind==ActionAnimationKind.Arrow),"Skeleton did not fire from range.");
        g.EnemyAi.ActEnemy(e);Check(p.Health==health,"Skeleton ignored reload turn.");g.EnemyAi.ActEnemy(e);Check(p.Health<health,"Skeleton never fired after reload.");
        e=Setup('s',4);d.Tiles[22,13]='#';g.EnemyAi.ActEnemy(e);Check(p.Health==1000,"Ranged attack crossed wall.");
        e=Setup('s',4);g.EnemyAi.ActEnemy(e,mayAttack:false);Check(p.Health==1000,"Enemy ignored attack-entry grace.");
        e=Setup('s',4);d.BlacksmithRoom=new Rect2I(19,12,3,3);g.EnemyAi.ActEnemy(e);Check(p.Health==1000,"Enemy shot into sanctuary.");
        e=Setup('g',4);g.EnemyAi.ActEnemy(e);Check(p.Health==1000&&e.Position!=origin,"Melee enemy gained ranged attack.");
        foreach(char glyph in "fmb")
        {
            e=Setup(glyph,glyph=='b'?3:1,11);g.CombatService.ResolveEnemyAttack(e,false);Check(w.HeroPoisonTurns==3,"Fungal poison missing.");
            e=Setup(glyph,1,11);g.RandomGenerator=new ItemRandom(20,.99);g.CombatService.ResolveEnemyAttack(e,false);Check(w.HeroPoisonTurns==0,"Fungal poison chance ignored.");
            e=Setup(glyph,1,11);g.InventoryState.Equipped[1]=NamedItemCatalog.Create(ItemId.FungalSovereignCrown);g.CombatService.ResolveEnemyAttack(e,false);Check(w.HeroPoisonTurns==0,"Crown failed against fungal attacks.");
            e=Setup(glyph,1,11);g.RandomGenerator=new ItemRandom(1,0);g.CombatService.ResolveEnemyAttack(e,false);Check(w.HeroPoisonTurns==0,"Miss applied poison.");
        }
        e=Setup('l',1,6);e.Health=1;g.CombatService.ResolveEnemyAttack(e,false);Check(e.Health==3,"Leech did not heal on hit.");
        e=Setup('e',1,6);w.Details[p.Position]='~';g.CombatService.ResolveEnemyAttack(e,false);Check(p.Energy==98,"Wet eel did not drain energy.");
        e=Setup('e',1,6);w.Details[p.Position]='~';g.InventoryState.Equipped[1]=NamedItemCatalog.Create(ItemId.ThickRubberBoots);g.CombatService.ResolveEnemyAttack(e,false);Check(p.Energy==100,"Rubber boots did not stop eel drain.");
        e=Setup('i',3,16);g.CombatService.ResolveEnemyAttack(e,false);Check(w.Fire.ContainsKey(p.Position),"Imp did not ignite ground.");
        e=Setup('i',3,16);g.InventoryState.Equipped[1]=NamedItemCatalog.Create(ItemId.EternalForgeRobe);g.CombatService.ResolveEnemyAttack(e,false);Check(p.Health==1000,"Robe failed against fire bolt.");
        e=Setup('a',1,16);w.Fire[e.Position]=4;health=e.Health;g.EnvironmentService.Tick();Check(e.Health==health,"Salamander burned on fire terrain.");
        foreach(char glyph in "qk")
        {
            e=Setup(glyph);e.Health=1000;g.RandomGenerator=new ItemRandom(10);
            int damage=TabletopRules.RollDamage(new ItemRandom(10),g.HeroCombatStats.MeleeDice,false);
            g.CombatService.ResolveHeroAttack(e);Check(e.Health==1000-Math.Max(1,damage-1),"Armored monster did not reduce physical hit.");
        }
        GD.Print("ENEMY TRAITS AUDIT: ranged attacks/reload, walls, entry grace, sanctuary, poison chance/immunity/misses, leech healing, wet eel/boots, imp/robe, salamander fire and armored creatures passed.");
    }
}
