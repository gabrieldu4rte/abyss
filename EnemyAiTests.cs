using Godot;
using System;
using System.Linq;

public partial class Main
{
    void TestEnemyAi()
    {
        selected=0;Start(91);enemies.Clear();rng=new FixedRandom(1);
        for(int x=0;x<W;x++)for(int y=0;y<H;y++)map[x,y]=x==0||y==0||x==W-1||y==H-1?'#':'.';
        player=new Vector2I(50,20);
        var scout=new Enemy(new Vector2I(3,4),'g',1){PatrolTarget=new Vector2I(8,4)};
        enemies.Add(scout);ActEnemy(scout);
        if(scout.P!=new Vector2I(4,4)||scout.Alerted)throw new Exception("Patrol did not move while unaware");
        player=new Vector2I(8,4);ActEnemy(scout);
        if(!scout.Alerted||scout.P!=new Vector2I(5,4)||scout.LastSeen!=player)throw new Exception("Visible player not pursued immediately");
        for(int y=1;y<=15;y++)map[10,y]='#';
        player=new Vector2I(12,4);ActEnemy(scout);
        if(scout.LastSeen!=new Vector2I(8,4)||scout.P!=new Vector2I(6,4))throw new Exception("Enemy tracked player through wall");
        for(int i=0;i<8;i++)ActEnemy(scout);
        if(scout.Alerted)throw new Exception("Search memory never expired");
        enemies.Clear();scout=new Enemy(new Vector2I(8,4),'r',1);enemies.Add(scout);
        player=new Vector2I(50,20);
        for(int i=0;i<40&&scout.P!=new Vector2I(12,4);i++)StepEnemy(scout,new Vector2I(12,4));
        if(scout.P!=new Vector2I(12,4))throw new Exception("Pathfinding failed to route around wall");
        var blocker=new Enemy(new Vector2I(13,4),'s',1);enemies.Add(blocker);
        if(StepEnemy(scout,blocker.P)||scout.P==blocker.P)throw new Exception("Enemies overlapped");

        enemies.Clear();stairsRoom=new Rect2I(20,10,6,5);stairs=new Vector2I(23,12);
        var boss=new Enemy(new Vector2I(20,12),'B',5);enemies.Add(boss);
        player=new Vector2I(19,12);hp=maxHp;
        int beforeHp=hp;ActEnemy(boss);
        if(boss.Alerted||boss.P!=new Vector2I(20,12)||hp!=beforeHp)throw new Exception("Boss activated outside its room");
        player=new Vector2I(21,12);ActEnemy(boss);
        if(!boss.Alerted||!log.Any(e=>e.En.Contains("Warden awakens")))throw new Exception("Boss did not activate on room entry");
        player=new Vector2I(19,12);beforeHp=hp;
        for(int i=0;i<15;i++)
        {
            ActEnemy(boss);
            if(!stairsRoom.HasPoint(boss.P)||hp!=beforeHp)throw new Exception("Boss left room or attacked outside it");
        }
        if(boss.P!=stairs)throw new Exception("Boss did not return to stairs");
        player=new Vector2I(25,14);ActEnemy(boss);
        if(boss.P==stairs)throw new Exception("Boss did not resume pursuit after reentry");

        for(int seed=1;seed<=20;seed++)
        {
            Start(seed);floor=10;Generate();hp=100000;
            var guardian=enemies.Single(e=>e.Glyph=='B');
            for(int i=0;i<30;i++)
            {
                EndTurn();
                if(enemies.Any(e=>!Walk(e.P)||e.P==player)||enemies.Select(e=>e.P).Distinct().Count()!=enemies.Count)throw new Exception("Patrol movement violated occupancy");
                if(guardian.Alerted||guardian.P!=stairs)throw new Exception("Distant boss activated during patrol");
            }
        }
        selected=0;Start(50);enemies.Clear();rng=new FixedRandom(20);
        for(int x=1;x<W-1;x++)for(int y=1;y<H-1;y++)map[x,y]='.';
        player=new Vector2I(5,5);var melee=new Enemy(new Vector2I(7,5),'r',1);enemies.Add(melee);
        int health=hp;ResolveEnemyAttack(melee,false);
        if(hp!=health)throw new Exception("Enemy attacked from two tiles away");
        melee.P=new Vector2I(6,6);ResolveEnemyAttack(melee,false);
        if(hp!=health)throw new Exception("Enemy attacked diagonally");
        melee.P=new Vector2I(7,5);Move(Vector2I.Right);
        if(hp!=health||player!=new Vector2I(6,5))throw new Exception("Enemy attacked immediately on player approach");
        EndTurn();if(hp>=health)throw new Exception("Adjacent enemy failed to attack on next action");
        player=new Vector2I(5,5);melee.P=new Vector2I(7,5);health=hp;ActEnemy(melee);
        if(hp!=health||Dist(player,melee.P)!=1)throw new Exception("Enemy moved and attacked in the same action");
        for(int hero=1;hero<=2;hero++)
        {
            selected=hero;Start(90);enemies.Clear();
            var delta=Directions.First(d=>Walk(player+d));
            var victim=new Enemy(player+delta,'r',1){Hp=100};enemies.Add(victim);
            int beforeTurn=turn,beforeEnergy=energy;var origin=player;
            Move(delta);ResolveHeroAttack(victim);
            if(victim.Hp!=100||turn!=beforeTurn||energy!=beforeEnergy||player!=origin)throw new Exception("Ranged class performed a melee attack");
            equipped[0]=null;rng=new FixedRandom(20);Move(delta);
            if(victim.Hp!=96||turn!=beforeTurn+1||CanShoot||!CanMelee)throw new Exception("Unarmed ranged class could not attack with 1d2");
            equipped[0]=backpack[0];int remaining=victim.Hp;beforeTurn=turn;Move(delta);
            if(victim.Hp!=remaining||turn!=beforeTurn||CanMelee)throw new Exception("Equipping ranged weapon did not disable melee");
        }
        GD.Print("AI AUDIT: patrol, visual detection, last-seen search, wall routing, occupancy, boss activation and confinement passed; 600 generated-map turns.");
    }
}
