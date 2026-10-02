using Godot;
using System;
using System.Linq;
namespace Abyss.Tests;
internal sealed partial class RegressionSuite
{
    internal void TestAdvancement()
    {
        static void Check(bool value,string message) { if (!value) throw new Exception(message); }
        var test = new GameSession(new TestHost(),new TestCanvas(),new TestSettings());
        for (int hero=0;hero<4;hero++)
        {
            test.PlayerState.ClassIndex=hero; test.Start(811);
            Check(!test.ClassAdvancementService.Choose(AdvancementCatalog.Options(hero)[0]),"Early advancement allowed.");
            test.PlayerState.Level=9; test.PlayerState.Experience=test.HeroCombatStats.XpToNext-1;
            test.ProgressionService.GainXp(1);
            Check(test.PlayerState.Level==10 && test.ClassAdvancementService.Available,"Level ten did not unlock advancement.");
            int turn=test.RunState.Turn,energy=test.PlayerState.Energy;
            test.ClassAdvancementService.Offer();
            Check(test.RunState.Screen=="advancement","Automatic advancement offer missing.");
            test.GameInput.HandleKey(Key.Enter); test.GameInput.HandleKey(Key.Enter);
            Check(test.PlayerState.AdvancedClass==AdvancedClass.None,"Default advancement choice should cancel.");
            test.GameInput.HandleKey(Key.Escape); test.ClassAdvancementService.Offer();
            Check(test.RunState.Screen=="game","Deferred advancement reopened immediately.");
            test.GameInput.HandleKey(Key.Escape); test.GameInput.HandleKey(Key.C);
            Check(test.RunState.Screen=="advancement","Character sheet cannot reopen advancement.");
            test.GameInput.HandleKey(Key.Right); test.GameInput.HandleKey(Key.Enter); test.GameInput.HandleKey(Key.Down); test.GameInput.HandleKey(Key.Enter);
            Check(test.PlayerState.AdvancedClass==AdvancementCatalog.Options(hero)[1] && test.RunState.Screen=="pause" && test.PlayerState.ClassIndex==hero && test.RunState.Turn==turn && test.PlayerState.Energy==energy,"Advancement changed base class or consumed resources.");
            Check(!test.ClassAdvancementService.Choose(AdvancementCatalog.Options(hero)[0]),"Advancement can be chosen twice.");
            foreach (var choice in AdvancementCatalog.Options(hero))
            {
                test.Start(811);test.PlayerState.Level=10;
                var before=test.HeroCombatStats.AbilityDice;
                Check(test.ClassAdvancementService.Choose(choice),"Valid specialization rejected.");
                Check(test.HeroCombatStats.AbilityDice.Count*test.HeroCombatStats.AbilityDice.Sides>before.Count*before.Sides,"Primary skill damage did not improve.");
                test.PlayerState.Health=test.PlayerState.MaxHealth;
                string normal=HeroPortrait.Select(test.PlayerState);
                test.PlayerState.Health=Math.Max(1,test.PlayerState.MaxHealth/4);
                string critical=HeroPortrait.Select(test.PlayerState);
                Check(normal!=critical && normal!=AsciiArt.Heroes[hero] && normal.Split('\n').Length==60 && critical.Split('\n').Length==60,"Advanced portraits are missing or identical.");
                Check(normal.Concat(critical).All(c=>c=='\n'||c is >= ' ' and <= '~'),"Advanced portrait is not ASCII.");
            }
        }
        void Prepare(int hero,AdvancedClass choice=AdvancedClass.None)
        {
            test.PlayerState.ClassIndex=hero;test.Start(910);
            test.DungeonState.Enemies.Clear();test.DungeonState.Items.Clear();test.DungeonState.Environment.Clear();
            test.DungeonState.BlacksmithRoom=null;test.DungeonState.IsMerchantFloor=false;
            test.PlayerState.Level=choice==AdvancedClass.None ? 1 : 10;
            test.PlayerState.AdvancedClass=choice;
            test.PlayerState.Health=test.PlayerState.MaxHealth=200;
            test.PlayerState.Energy=test.PlayerState.MaxEnergy=100;
            test.PlayerState.Position=new Vector2I(30,13);
            for(int y=1;y<GameRules.Height-1;y++) for(int x=1;x<GameRules.Width-1;x++) test.DungeonState.Tiles[x,y]='.';
            test.DungeonGenerator.Reveal();test.RandomGenerator=new FixedRandom(20);
        }
        Enemy Place(Vector2I offset,int depth=1,char glyph='s')
        {
            var enemy=new Enemy(test.PlayerState.Position+offset,glyph,depth) { Health=10000,MaxHealth=10000 };
            test.DungeonState.Enemies.Add(enemy);return enemy;
        }
        foreach(var direction in GameRules.Directions)
        {
            Prepare(3);var origin=test.PlayerState.Position;var far=Place(direction*2);
            test.PlayerActions.Skill();
            Check(test.RunState.Turn==0 && test.PlayerState.Energy==100 && far.Health==10000,"Rogue can strike non-adjacent targets.");
            far.Position=origin+direction;
            test.PlayerActions.Skill();
            Check(test.PlayerState.Position==origin+direction*2 && far.Health<10000 && test.RunState.Turn==1,"Rogue hit or behind-target teleport failed.");
            Check(test.VisualEffects.Actions.Animations.Single().Path[^1]==test.PlayerState.Position,"Rogue animation missed teleport destination.");
        }
        foreach(int blocker in Enumerable.Range(0,5))
        {
            Prepare(3);var origin=test.PlayerState.Position;var foe=Place(Vector2I.Right);var behind=origin+Vector2I.Right*2;
            if(blocker==0)test.DungeonState.Tiles[behind.X,behind.Y]='#';
            if(blocker==1)Place(Vector2I.Right*2);
            if(blocker==2)test.DungeonState.Environment.Fixtures[behind]=Fixture.OilBarrel;
            if(blocker==3)test.DungeonState.Environment.Fixtures[behind]=Fixture.WallTorch;
            if(blocker==4){test.DungeonState.BlacksmithRoom=new Rect2I(behind,new Vector2I(3,3));test.DungeonState.BlacksmithPosition=behind;}
            test.PlayerActions.Skill();
            Check(test.PlayerState.Position==origin && foe.Health<10000,"Blocked teleport cancelled damage or occupied a blocked cell.");
        }
        Prepare(3);var missOrigin=test.PlayerState.Position;var missed=Place(Vector2I.Right);test.RandomGenerator=new FixedRandom(1);test.PlayerActions.Skill();
        Check(test.PlayerState.Position==missOrigin && missed.Health==10000,"A missed rogue strike teleported.");
        Prepare(3);var dyingOrigin=test.PlayerState.Position;Place(Vector2I.Right);test.PlayerState.Health=1;test.InventoryState.Equipped[0]=NamedItemCatalog.Create(ItemId.ExecutionerBlade);test.PlayerActions.Skill();
        Check(test.RunState.Screen=="dead" && test.PlayerState.Position==dyingOrigin,"A lethal self-cost still teleported the rogue.");
        foreach(var choice in Enum.GetValues<AdvancedClass>().Where(c=>c!=AdvancedClass.None))
        {
            int hero=AdvancementCatalog.Get(choice).BaseClass;Prepare(hero,choice);
            int range=test.HeroCombatStats.SecondaryRange;
            var foe=choice==AdvancedClass.Sentinel ? null : Place(Vector2I.Right*Math.Min(range,3));
            if(choice==AdvancedClass.Berserker) foe!.BlindTurns=1;
            int cost=test.HeroCombatStats.SecondaryCost;
            test.PlayerState.Energy=cost-1;test.AdvancedAbilityService.Use();
            Check(test.RunState.Turn==0 && test.PlayerState.Energy==cost-1,"Unaffordable advanced skill consumed a turn.");
            test.PlayerState.Energy=100;test.AdvancedAbilityService.Use();
            Check(test.RunState.Turn==1 && test.PlayerState.Energy==100-cost && test.VisualEffects.Actions.Active,"Advanced skill did not spend exactly one turn and its cost.");
            if(choice==AdvancedClass.Sentinel)Check(test.PlayerState.GuardTurns==2 && test.PlayerState.GuardBonus==6,"Bastion duration failed.");
            else if(choice==AdvancedClass.Shadowblade)Check(foe!.BlindTurns==1 && foe.Health==10000,"Shadow veil should blind without damage.");
            else Check(foe!.Health<10000,"Advanced damaging ability missed a natural 20.");
            if(choice==AdvancedClass.Cryomancer)Check(foe!.FrozenTurns==1,"Frozen prison did not suppress the response turn.");
            if(choice==AdvancedClass.Pyromancer)Check(test.DungeonState.Environment.Fire.Count>0,"Fireburst did not ignite its area.");
            if(choice==AdvancedClass.Assassin)Check(test.DungeonState.Environment.PoisonedEnemies.ContainsKey(foe!),"Venom blade did not apply poison.");
            if(choice==AdvancedClass.Berserker)Check(GameRules.Dist(test.PlayerState.Position,foe!.Position)==2,"Crushing blow displaced incorrectly.");
            int savedTurn=test.RunState.Turn,savedEnergy=test.PlayerState.Energy;
            test.VisualEffects.AdvanceEffects(2);
            Check(test.RunState.Turn==savedTurn && test.PlayerState.Energy==savedEnergy,"Advanced animations changed gameplay.");
            Prepare(hero,choice);
            if(choice!=AdvancedClass.Sentinel){test.AdvancedAbilityService.Use();Check(test.RunState.Turn==0 && test.PlayerState.Energy==100,"Targetless advanced skill consumed resources.");}
        }
        Prepare(3,AdvancedClass.Assassin);var fungus=Place(Vector2I.Right,11,'f');test.AdvancedAbilityService.Use();
        Check(!test.DungeonState.Environment.PoisonedEnemies.ContainsKey(fungus),"Fungal immunity ignored by venom blade.");
        Prepare(3,AdvancedClass.Shadowblade);Place(Vector2I.Right);test.PlayerActions.Skill();
        Check(test.PlayerState.GuardBonus==4 && test.PlayerState.GuardTurns==1,"Shadowblade blink did not grant its temporary defense.");
        test.Start(19);Check(test.PlayerState.AdvancedClass==AdvancedClass.None && test.PlayerState.GuardTurns==0,"Advancement leaked into a new expedition.");
        GD.Print("ADVANCEMENT AUDIT: four two-way choices, level ten, deferral, one-time confirmation, sixteen ASCII portraits, eight new skills, costs, effects, fungal immunity and adjacent rogue teleport/blockers passed.");
    }
}
