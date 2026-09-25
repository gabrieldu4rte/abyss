using Godot;
using System;
using System.Linq;
using static TabletopRules;

public partial class Main
{
	sealed class FixedRandom : Random
	{
		readonly int value;
		public FixedRandom(int value){this.value=value;}
		public override int Next(int minValue,int maxValue)=>Math.Clamp(value,minValue,maxValue-1);
		public override double NextDouble()=>.5;
	}
	void TestBalance()
	{
		if(ResolveAttack(1,100,2).Hit||!ResolveAttack(20,-100,99).Critical)throw new Exception("Natural 1/20 failed");
		if(HitChance(4,14)!=55||HitChance(100,1)!=95||HitChance(-100,99)!=5)throw new Exception("D20 probability failed");
		if(Modifier(9)!=-1||Modifier(15)!=2||Modifier(16)!=3)throw new Exception("Attribute modifier failed");
		if(RollDamage(new FixedRandom(6),new DamageDice(1,6,2),true)!=14)throw new Exception("Critical dice failed");
		var random=new Random(99);int sum=0;
		for(int i=0;i<10000;i++){var potion=RollPotion(random);if(potion.Total<2||potion.Total>20)throw new Exception("2d10 bounds failed");sum+=potion.Total;}
		if(Math.Abs(sum/10000.0-11)>.2)throw new Exception("2d10 mean failed");
		for(int c=0;c<4;c++)
		{
			selected=c;Start(77);enemies.Clear();
			int oldEnergy=energy,oldTurn=turn;
			if(!CanShoot)
			{
				BeginAim();Shoot(Vector2I.Right);
				if(aiming||energy!=oldEnergy||turn!=oldTurn)throw new Exception("Melee class can shoot");
			}
			else
			{
				int cost=ShotCost;Shoot(Vector2I.Right);
				if(energy!=oldEnergy-cost||turn!=oldTurn+1)throw new Exception("Shot cost failed");
				energy=0;oldTurn=turn;Shoot(Vector2I.Right);
				if(turn!=oldTurn+1||energy!=0)throw new Exception("Free basic shot failed at zero energy");
			}
			Start(77);enemies.Clear();int previousCost=AbilityCost;double previousDamage=AbilityDice.Average;
			for(int n=2;n<=8;n++)
			{
				GainXp(XpToNext);
				if(AbilityCost>previousCost||AbilityDice.Average<previousDamage)throw new Exception("Progression regressed");
				previousCost=AbilityCost;previousDamage=AbilityDice.Average;
			}
			Start(77);enemies.Clear();energy=0;oldTurn=turn;Skill();
			if(turn!=oldTurn)throw new Exception("Underfunded ability used a turn");
			var pos=Directions.Select(d=>player+d).First(Walk);
			var foe=new Enemy(pos,'g',1){Hp=999,MaxHp=999};enemies.Add(foe);Reveal();energy=maxEnergy;
			rng=new FixedRandom(1);int oldHp=foe.Hp,costQ=AbilityCost;Skill();
			if(foe.Hp!=oldHp||energy!=maxEnergy-costQ||turn!=1)throw new Exception("Miss must consume energy and turn");
			if(!log.Any(e=>e.En.Contains("You miss")))throw new Exception("Miss not logged");
			enemies.Clear();hp=1;potions=2;rng=new FixedRandom(1);Drink();
			if(lastPotionRoll!=2||lastPotionHealing!=2||hp!=3||potions!=1)throw new Exception("Minimum potion failed");
			hp=maxHp-1;rng=new FixedRandom(10);Drink();
			if(lastPotionRoll!=20||lastPotionHealing!=1||hp!=maxHp)throw new Exception("Potion clamping failed");
			potions=1;oldTurn=turn;Drink();if(potions!=1||turn!=oldTurn)throw new Exception("Full-health potion consumed");
		}
		foreach(char g in new[]{'r','s','g','B'})for(int depth=2;depth<=1000;depth++)
		{
			var old=new Enemy(Vector2I.Zero,g,depth-1);var current=new Enemy(Vector2I.Zero,g,depth);
			if(current.MaxHp<old.MaxHp||current.AttackBonus<old.AttackBonus||EnemyXp(g,depth)<EnemyXp(g,depth-1))throw new Exception("Enemy growth failed");
			bool newBand=(depth-1)%5==0;
			if(newBand&&(current.MaxHp<=old.MaxHp||current.Dice.Average<=old.Dice.Average))throw new Exception("Five-floor strength step missing");
			if(!newBand&&(current.MaxHp!=old.MaxHp||current.Dice!=old.Dice||current.Stats!=old.Stats||current.AttackBonus!=old.AttackBonus||current.Armor!=old.Armor))throw new Exception("Enemy stats changed inside a five-floor band");
		}
		foreach(var spec in new[]{('r',4,2.0),('s',6,2.5),('g',8,3.5),('B',24,5.0)})
		{
			var early=new Enemy(Vector2I.Zero,spec.Item1,spec.Item1=='B'?5:1);
			if(early.MaxHp!=spec.Item2||early.Dice.Average!=spec.Item3)throw new Exception("Starting enemy tuning changed");
		}
		selected=0;Start(77);var oldStats=attributes;hp=5;energy=0;GainXp(30+50+70+5);
		if(level!=4||xp!=5||attributes.Strength!=oldStats.Strength+2||attributes.Constitution!=oldStats.Constitution+1||hp>=maxHp||energy>=maxEnergy)throw new Exception("Slow level progression failed");
		enemies.Clear();var target=new Enemy(player+Vector2I.Right,'s',1){Hp=1};enemies.Add(target);Hit(target,2);
		if(!log.Any(e=>e.En.Contains("Skeleton")&&e.Pt.Contains("Esqueleto")))throw new Exception("Named journal failed");
		for(int seed=1;seed<=100;seed++)
		{
			Start(seed);
			if(potions!=5||items.Values.Count(g=>g=='!')>1||items.Values.Count(g=>g=='*')>1||items.Values.Count(g=>g=='$')!=2||items.Values.Count(g=>g=='C')>1)throw new Exception("Loot scarcity failed");
		}
		// A seeded simulation checks the advertised hit probability against real rolls.
		random=new Random(734);int hits=0;
		for(int i=0;i<20000;i++)if(ResolveAttack(random.Next(1,21),4,14).Hit)hits++;
		double measured=hits/20000.0;
		if(Math.Abs(measured-.55)>.015)throw new Exception("Simulated hit rate disagrees with probability");
		GD.Print($"DICE AUDIT: +4 vs AC14 = 55%; observed {measured:P1} over 20,000 attacks. 2d10 mean {sum/10000.0:F2}.");
	}
}
