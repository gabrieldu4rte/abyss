using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class Main
{
    void ActEnemy(Enemy enemy,bool evade=false,bool mayAttack=true)
    {
        if(enemy.Glyph=='B')
        {
            bool inside=stairsRoom.HasPoint(player);
            if(!enemy.Alerted)
            {
                if(!inside)return;
                enemy.Alerted=true;
                Say("Voce entrou na sala da escada. O Guardiao desperta!","You entered the stair room. The Warden awakens!");
            }
            // The guardian never attacks across the doorway or leaves its room.
            if(inside&&Dist(player,enemy.P)==1){if(mayAttack)ResolveEnemyAttack(enemy,evade);}
            else StepEnemy(enemy,inside?player:stairs,true);
            return;
        }

        bool seesPlayer=Dist(enemy.P,player)<13&&Los(enemy.P,player);
        if(seesPlayer)
        {
            enemy.Alerted=true;enemy.LastSeen=player;enemy.SearchTurns=6;enemy.PatrolTarget=null;
            if(Dist(player,enemy.P)==1){if(mayAttack)ResolveEnemyAttack(enemy,evade);}
            else StepEnemy(enemy,player);
            return;
        }
        // Follow the last known position, never the player's position through walls.
        if(enemy.Alerted&&enemy.LastSeen.HasValue&&enemy.SearchTurns>0&&enemy.P!=enemy.LastSeen.Value)
        {
            StepEnemy(enemy,enemy.LastSeen.Value);enemy.SearchTurns--;return;
        }
        enemy.Alerted=false;enemy.LastSeen=null;enemy.SearchTurns=0;
        if(!enemy.PatrolTarget.HasValue||enemy.PatrolTarget.Value==enemy.P)
        {
            var destinations=new List<Vector2I>();
            for(int y=1;y<H-1;y++)for(int x=1;x<W-1;x++)
            {
                var p=new Vector2I(x,y);
                if(Walk(p)&&p!=enemy.P&&p!=player&&At(p)==null)destinations.Add(p);
            }
            enemy.PatrolTarget=destinations.Count==0?null:destinations[rng.Next(destinations.Count)];
        }
        if(enemy.PatrolTarget.HasValue&&!StepEnemy(enemy,enemy.PatrolTarget.Value))enemy.PatrolTarget=null;
    }

    bool StepEnemy(Enemy enemy,Vector2I destination,bool stayInRoom=false)
    {
        if(enemy.P==destination)return true;
        var queue=new Queue<Vector2I>();
        var previous=new Dictionary<Vector2I,Vector2I>{{enemy.P,enemy.P}};
        queue.Enqueue(enemy.P);
        while(queue.Count>0&&!previous.ContainsKey(destination))
        {
            var a=queue.Dequeue();
            foreach(var direction in Directions)
            {
                var b=a+direction;
                if(!Walk(b)||previous.ContainsKey(b)||(stayInRoom&&!stairsRoom.HasPoint(b)))continue;
                if(At(b)!=null||(b==player&&b!=destination))continue;
                previous[b]=a;queue.Enqueue(b);
            }
        }
        if(!previous.ContainsKey(destination))return false;
        var step=destination;
        while(previous[step]!=enemy.P)step=previous[step];
        if(step==player)return false;
        enemy.P=step;return true;
    }
}
