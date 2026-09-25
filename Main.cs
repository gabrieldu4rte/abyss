using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using static TabletopRules;

public partial class Main : Node2D
{
	const int W = 64, H = 27;
	readonly char[,] map = new char[W,H];
	readonly bool[,] seen = new bool[W,H], visible = new bool[W,H];
	readonly List<Enemy> enemies = new();
	readonly Dictionary<Vector2I,char> items = new();
	readonly List<(string Pt, string En)> log = new();
	readonly string[] names = { "GUERREIRO", "MAGO", "ARQUEIRO", "LADINO" };
	readonly string[] skills = { "Redemoinho", "Nova arcana", "Flecha precisa", "Passo sombrio" };
	readonly Color ink = new("c6d2da"), dim = new("506174"), gold = new("e8b86b"), teal = new("65d9c0"), red = new("ef7c83");
	Random rng = new();
	Font font = null!;
	Vector2I player, stairs;
	Rect2I stairsRoom;
	int selected, floor, hp, maxHp, energy, maxEnergy, potions, xp, level, coins, turn, seed, kills;
	string screen = "home";
	bool aiming;
	double clock;
	sealed class Enemy
	{
		public Vector2I P;
		public int Hp,MaxHp,LastDamage,Depth;
		public double Hurt;
		public bool Alerted;
		public int SearchTurns;
		public Vector2I? PatrolTarget, LastSeen;
		public char Glyph;
		public Attributes Stats;
		public int Tier=>(Depth-1)/5;
		public int Training=>1+Tier/2;
		public int CombatModifier=>Glyph is 'r' or 'g'?Stats.Dex:Stats.Str;
		public int AttackBonus=>Training+CombatModifier;
		public int ArmorClass=>(Glyph=='B'?13:Glyph=='s'?11:10)+Tier/3;
		public int Armor=>ArmorClass+Stats.Con;
		public DamageDice Dice=>new(Glyph=='B'?2:1,Glyph is 'B' or 'r'?3:4,CombatModifier+Tier*(Glyph=='B'?2:1));
		public Enemy(Vector2I p,char glyph,int depth)
		{
			P=p;Glyph=glyph;Depth=depth;Stats=MonsterAttributes(glyph,depth);
			Hp=MaxHp=(glyph=='B'?24:glyph=='g'?8:glyph=='s'?6:4)+Tier*(glyph=='B'?8:3);
		}
	}

	public override async void _Ready()
	{
		font = GD.Load<Font>("res://Mono.ttf");
		var args=OS.GetCmdlineUserArgs();
		if(args.Contains("--self-test")) { SelfTest(); return; }
		LoadLanguage();
		var capture=args.FirstOrDefault(a=>a.StartsWith("--capture="));
		if(capture!=null)SetProcessUnhandledKeyInput(false);
		if(args.Contains("--english")) english=true;
		if(args.Contains("--portuguese")) english=false;
		if(args.Contains("--demo")) {selected=1;Start(42073);}
		var view=args.FirstOrDefault(a=>a.StartsWith("--view="));
		if(view!=null)
		{
			var name=view.Substring(7);
			if(name=="help"){screen="pause";pauseTab=3;}
			else if(name=="language")OpenLanguage("home");
			else if(name=="inventory"){screen="pause";pauseTab=1;}
			else if(name=="journal"||name=="settings"){screen="pause";pauseTab=name=="journal"?2:4;}
			else screen=name;
		}
		if(args.Contains("--damage-demo"))
		{
			selected=0;Start(42073);enemies.Clear();
			var pos=Directions.Select(d=>player+d).First(Walk);
			var foe=new Enemy(pos,'g',1){Hp=35,MaxHp=35};enemies.Add(foe);Reveal();ResolveHeroAttack(foe);EndTurn();
		}
		if(args.Contains("--ranged-demo"))
		{
			selected=1;Start(42073);enemies.Clear();
			var pos=Directions.Select(d=>player+d).First(Walk);
			var foe=new Enemy(pos,'s',2){Hp=35,MaxHp=35};enemies.Add(foe);Reveal();
			hp=maxHp-10;potions=1;Drink();
		}
		if(args.Contains("--inventory-demo"))
		{
			selected=0;Start(42073);level=10;potions=2;energyPotions=1;
			backpack.Add(new Gear(GearKind.Sword,Rarity.Rare));
			backpack.Add(new Gear(GearKind.Plate,Rarity.Epic));
			backpack.Add(new Gear(GearKind.Sword,Rarity.Legendary));
			backpack.Add(new Gear(GearKind.Amulet,Rarity.Epic));
			screen="pause";pauseTab=1;inventoryIndex=8;
		}
		if(args.Contains("--merchant-demo"))
		{
			selected=0;Start(123);floor=16;coins=200;Generate(true);player=merchantPosition+Vector2I.Down;Interact();
			if(args.Contains("--trade-demo"))HandleShop(Key.Enter);
			if(args.Contains("--room-demo"))screen="game";
		}
		QueueRedraw();
		if(capture!=null)
		{
			var delay=args.FirstOrDefault(a=>a.StartsWith("--capture-delay="));
			if(delay!=null && double.TryParse(delay.Substring(16),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out double seconds))
				await ToSignal(GetTree().CreateTimer(Math.Clamp(seconds,0,3)),SceneTreeTimer.SignalName.Timeout);
			await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
			await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
			GetViewport().GetTexture().GetImage().SavePng(capture.Substring(10));
			GetTree().Quit();
		}
	}
	public override void _Process(double delta)
	{
		clock+=delta;uiTime+=delta;
		AdvanceHeldMovement(delta);
		if(screen=="game"||screen=="dead") AdvanceEffects(delta);
		if(clock>.10) {clock=0;QueueRedraw();}
	}
	void Say(string pt,string en) {log.Insert(0,(pt,en));}
	void Start(int? fixedSeed = null)
	{
		seed = fixedSeed ?? Random.Shared.Next(1,int.MaxValue);
		rng = new Random(seed); floor=1; level=1; xp=coins=turn=kills=0; potions=5;
		attributes=HeroAttributes(selected);
		maxHp=new[]{28,26,26,28}[selected]+Math.Max(0,attributes.Con)*4;hp=maxHp;
		maxEnergy=new[]{9,14,10,9}[selected]; energy=maxEnergy;
		lastRollPt=lastRollEn="";lastPotionRoll=lastPotionHealing=0;
		screen="game"; aiming=false; log.Clear(); ResetInventory(); ResetEffects(); Generate();
		Say("Desca o mais longe possivel.","Descend as far as you can."); Say("Voce entrou no Abismo. Cada passo conta.","You entered the Abyss. Every step counts.");
	}
	static bool IsBossFloor(int depth)=>depth%5==0;
	static int Dist(Vector2I a, Vector2I b) => Math.Abs(a.X-b.X)+Math.Abs(a.Y-b.Y);
	bool Inside(Vector2I p) => p.X>0 && p.Y>0 && p.X<W-1 && p.Y<H-1;
	bool Walk(Vector2I p) => Inside(p) && map[p.X,p.Y]!='#';
	Enemy? At(Vector2I p) => enemies.Find(e=>e.P==p);
	void Generate(bool? merchantOverride=null)
	{
		ResetEffects(); enemies.Clear(); items.Clear(); Array.Clear(seen); Array.Clear(visible);
		for(int x=0;x<W;x++) for(int y=0;y<H;y++) map[x,y]='#';
		merchantFloor=false;merchantStock.Clear();pendingTrade=null;
		if(merchantOverride??(MerchantEligible&&rng.NextDouble()<.10)){GenerateMerchantRoom();return;}
		var rooms=new List<Rect2I>();
		for(int tries=0;tries<160 && rooms.Count<10;tries++)
		{
			int rw=rng.Next(6,13), rh=rng.Next(4,8);
			var room=new Rect2I(rng.Next(2,W-rw-1),rng.Next(2,H-rh-1),rw,rh);
			if(rooms.Any(r=>r.Grow(1).Intersects(room))) continue;
			for(int x=room.Position.X;x<room.End.X;x++) for(int y=room.Position.Y;y<room.End.Y;y++) map[x,y]='.';
			if(rooms.Count>0)
			{
				var a=rooms[^1].GetCenter(); var b=room.GetCenter();
				while(a.X!=b.X) { map[a.X,a.Y]='.'; a.X+=Math.Sign(b.X-a.X); }
				while(a.Y!=b.Y) { map[a.X,a.Y]='.'; a.Y+=Math.Sign(b.Y-a.Y); }
				map[a.X,a.Y]='.';
			}
			rooms.Add(room);
		}
		stairsRoom=rooms[^1];
		player=rooms[0].GetCenter(); stairs=stairsRoom.GetCenter(); map[stairs.X,stairs.Y]='>';
		var free=new List<Vector2I>();
		for(int y=1;y<H-1;y++) for(int x=1;x<W-1;x++)
		{ var p=new Vector2I(x,y); if(Walk(p) && p!=stairs && Dist(p,player)>6) free.Add(p); }
		Vector2I Take() { int i=rng.Next(free.Count); var p=free[i]; free.RemoveAt(i); return p; }
		// Reserve room for scarce loot; density stays bounded at every depth.
		int population=Math.Min(7+(floor-1)/2,14)-(IsBossFloor(floor)?2:0);
		for(int i=0;i<Math.Min(population,Math.Max(0,free.Count-5));i++)
		{
			char g=i%3==0?'s':i%3==1?'r':'g';
			enemies.Add(new Enemy(Take(),g,floor));
		}
		for(int i=0;i<2;i++) items[Take()]='$';
		if(rng.NextDouble()<.75)items[Take()]='!';
		if(rng.NextDouble()<.40)items[Take()]='*';
		if(free.Count>0&&rng.NextDouble()<ChestChance(floor))items[Take()]='C';
		if(IsBossFloor(floor))
		{
			enemies.Add(new Enemy(stairs,'B',floor));
			Say($"Andar {floor}: um Guardiao bloqueia a descida.",$"Floor {floor}: a Warden blocks the descent.");
		}
		Reveal();
	}
	bool Los(Vector2I a, Vector2I b)
	{
		int x=a.X,y=a.Y,dx=Math.Abs(b.X-x),dy=-Math.Abs(b.Y-y),sx=x<b.X?1:-1,sy=y<b.Y?1:-1,err=dx+dy;
		while(true)
		{
			if(x==b.X && y==b.Y) return true;
			if((x!=a.X||y!=a.Y)&&map[x,y]=='#') return false;
			int e=err*2; if(e>=dy) {err+=dy;x+=sx;} if(e<=dx) {err+=dx;y+=sy;}
		}
	}
	void Reveal()
	{
		Array.Clear(visible);
		for(int y=0;y<H;y++) for(int x=0;x<W;x++)
		{ var p=new Vector2I(x,y); if((p-player).LengthSquared()<=100 && Los(player,p)) seen[x,y]=visible[x,y]=true; }
	}
	public override void _UnhandledKeyInput(InputEvent e)
	{
		if(e is not InputEventKey k || k.Echo) return;
		if(!k.Pressed){if(k.Keycode==heldMovementKey)StopHeldMovement();return;}
		StopHeldMovement();
		if(screen=="game"&&!aiming&&MovementDirection(k.Keycode)!=Vector2I.Zero)
		{heldMovementKey=k.Keycode;movementDelay=.30;}
		HandleKey(k.Keycode);QueueRedraw();
	}
	void HandleKey(Key key)
	{
		if(key==Key.F11) {DisplayServer.WindowSetMode(DisplayServer.WindowGetMode()==DisplayServer.WindowMode.Fullscreen?DisplayServer.WindowMode.Windowed:DisplayServer.WindowMode.Fullscreen);return;}
		if(HandleMenus(key)) return;
		if(key==Key.I){screen="pause";pauseTab=1;inventoryIndex=0;inventoryNotice=("","");return;}
		if(key==Key.Escape) {if(aiming)aiming=false;else {screen="pause";menuIndex=0;pauseTab=0;journalPage=0;}return;}
		if(key==Key.Tab) {CycleTarget();return;}
		Vector2I d=MovementDirection(key);
		if(d!=Vector2I.Zero)
		{
			if(aiming) {Shoot(d);aiming=false;}
			else Move(d);
		}
		else if(key==Key.F) BeginAim();
		else if(key==Key.Q) Skill();
		else if(key==Key.P) Drink();
		else if(key==Key.Space) {Say("Voce espera e recupera 1 de energia.","You wait and recover 1 energy.");energy=Math.Min(maxEnergy,energy+1);EndTurn();}
		else if(key==Key.E) Interact();
	}
	void Move(Vector2I d)
	{
		var previousPlayer=player;
		var p=player+d; if(!Walk(p)) return;
		if(merchantFloor&&p==merchantPosition){Say("[E] Conversar com o mercador.","[E] Talk to the merchant.");return;}
		var enemy=At(p);
		if(enemy!=null)
		{
			if(!CanMelee){Say("Use [F] para disparar ou [Q] para a habilidade.","Use [F] to shoot or [Q] for your ability.");return;}
			ResolveHeroAttack(enemy);
		}
		else {player=p;Pickup();}
		EndTurn(previousPlayer:previousPlayer);
	}
	void Pickup()
	{
		if(!items.Remove(player,out char g)) return;
		if(g=='C'){OpenChest();return;}
		if(g=='$') { int n=rng.Next(3,9);coins+=n;Say($"Tesouro: +{n} moedas.",$"Treasure: +{n} gold."); }
		if(g=='!') {potions++;Say("Pocao encontrada. [P] para beber.","Potion found. Press [P] to drink.");}
		if(g=='*') {energy=Math.Min(maxEnergy,energy+5);Say("Cristal: +5 energia.","Crystal: +5 energy.");}
	}
	void Hit(Enemy e,int damage,bool report=true)
	{
		e.Hp-=damage;EnemyHurt(e,damage);
		if(report)Say($"Voce atinge {MonsterName(e.Glyph,false)} por {damage}.",$"You hit {MonsterName(e.Glyph,true)} for {damage} damage.");
		if(e.Hp>0)return;
		enemies.Remove(e);kills++;
		if(equipped[2] is Gear charm&&charm.Quality>=Rarity.Epic)energy=Math.Min(maxEnergy,energy+(charm.Quality==Rarity.Legendary?2:1));
		int goldReward=RollEnemyGold(e.Glyph,e.Depth);coins+=goldReward;int reward=EnemyXp(e.Glyph,e.Depth);
		if(goldReward>0)Say($"{MonsterName(e.Glyph,false)} deixou {goldReward} ouro.",$"{MonsterName(e.Glyph,true)} dropped {goldReward} gold.");
		Say($"{MonsterName(e.Glyph,false)} derrotado. +{reward} XP.",$"{MonsterName(e.Glyph,true)} defeated. +{reward} XP.");
		if(e.Glyph=='B')
		{
			potions++;energy=Math.Min(maxEnergy,energy+3);
			var loot=DropEquipment(e.Depth,true);
			Say($"Guardiao: {GearNameFor(loot,false)}.",$"Warden: {GearNameFor(loot,true)}.");
			Say("Guardiao derrotado! +1 pocao, +3 energia. A descida esta livre.","Warden defeated! +1 potion, +3 energy. The descent is open.");
		}
		GainXp(reward);
	}
	void Shoot(Vector2I d)
	{
		if(!CanShoot){Say("Equipe um cajado ou arco compativel para disparar.","Equip a compatible staff or bow to shoot.");return;}
		int range=selected==2?10:6;
		
		var p=player;
		for(int i=0;i<range;i++){p+=d;if(!Walk(p))break;var e=At(p);if(e!=null){ResolveHeroAttack(e,true);EndTurn();return;}}
		Say("O disparo se perde na escuridao.","The shot fades into the darkness.");EndTurn();
	}
	void Skill()
	{
		int cost=AbilityCost;
		if(energy<cost){Say($"Habilidade requer {cost} de energia.",$"Your ability requires {cost} energy.");return;}
		var targets=enemies.Where(e=>visible[e.P.X,e.P.Y]&&Dist(player,e.P)<=AbilityRange).OrderBy(e=>Dist(player,e.P)).ToList();
		if(targets.Count==0){Say("Nenhum alvo ao alcance da habilidade.","No target within ability range.");return;}
		energy-=cost;Say(skills[selected]+"!",EnglishSkills[selected]+"!");
		if(selected<2)foreach(var e in targets)ResolveHeroAttack(e,false,true);
		else ResolveHeroAttack(focus!=null&&targets.Contains(focus)?focus:targets[0],false,true);
		EndTurn(selected==3);
	}
	void Drink()
	{
		if(potions==0) {Say("Voce nao tem pocoes.","You have no potions.");return;}
		if(hp==maxHp) {Say("Sua vida ja esta cheia.","Your health is already full.");return;}
		var healing=RollPotion(rng);lastPotionRoll=healing.Total;lastPotionHealing=Math.Min(maxHp-hp,healing.Total);
		potions--;hp+=lastPotionHealing;
		Say($"Pocao: 2d10 [{healing.First}+{healing.Second}] = {healing.Total}. Curou {lastPotionHealing} PV.",$"Potion: 2d10 [{healing.First}+{healing.Second}] = {healing.Total}. Healed {lastPotionHealing} HP.");
		EndTurn();
	}
	void Descend()
	{
		if(player!=stairs) {Say("Procure a escada [>] e pise nela.","Find the stairs [>] and stand on them.");return;}
		if(IsBossFloor(floor)&&enemies.Any(e=>e.Glyph=='B'))
		{Say("O Guardiao ainda bloqueia a descida.","The Warden still blocks the descent.");return;}

		floor++;hp=Math.Min(maxHp,hp+2);energy=Math.Min(maxEnergy,energy+3);Generate();Say($"Andar {floor}. +2 PV, +3 energia.",$"Floor {floor}. +2 HP, +3 energy.");
	}
	void EndTurn(bool evade=false,Vector2I? previousPlayer=null)
	{
		turn++; Reveal();
		foreach(var e in enemies.ToArray())
		{
			ActEnemy(e,evade,!previousPlayer.HasValue||previousPlayer.Value==player||Dist(previousPlayer.Value,e.P)==1);
			if(hp<=0)break;
		}
		if(turn%6==0) energy=Math.Min(maxEnergy,energy+1);
		Reveal();
	}
	static readonly Vector2I[] Directions={Vector2I.Up,Vector2I.Down,Vector2I.Left,Vector2I.Right};
	void Text(float x,float y,string s,Color? c=null,int size=18) => DrawString(font,new Vector2(x,y),s,HorizontalAlignment.Left,-1,size,c??ink);
	void Lines(float x,float y,string s,Color c,int size=18,int spacing=23)
	{foreach(string line in s.Split('\n')) {Text(x,y,line,c,size);y+=spacing;}}
	void Rule(float y) => Text(32,y,new string('-',110),dim);
	string Bar(int value,int max,int length=18) {int n=Math.Clamp(value*length/Math.Max(1,max),0,length);return "["+new string('#',n)+new string('.',length-n)+"]";}
	void SelfTest()
	{
		Hide();
		try
		{
			for(int s=1;s<=50;s++) foreach(int f in new[]{1,4,5,6,10,15,25,50,100,500,1000})
			{
				selected=s%4;Start(s);floor=f;Generate();
				var reached=new HashSet<Vector2I>{player};var q=new Queue<Vector2I>();q.Enqueue(player);
				while(q.Count>0) {var a=q.Dequeue();foreach(var d in Directions){var b=a+d;if(Walk(b)&&reached.Add(b))q.Enqueue(b);}}
				if(!reached.Contains(stairs)||enemies.Any(e=>!reached.Contains(e.P))||items.Keys.Any(p=>!reached.Contains(p)))throw new Exception("Unreachable content");
				if(enemies.Select(e=>e.P).Distinct().Count()!=enemies.Count||enemies.Any(e=>items.ContainsKey(e.P)||e.P==player))throw new Exception("Overlapping entities");
				for(int x=0;x<W;x++) if(map[x,0]!='#'||map[x,H-1]!='#')throw new Exception("Open border");
				if(enemies.Count(e=>e.Glyph=='B')!=(IsBossFloor(f)?1:0)||enemies.Count>15)throw new Exception("Boss cadence or density failed");
			rng=new Random(s);floor=f;Generate();
				var first=new string(map.Cast<char>().ToArray());rng=new Random(s);Generate();if(first!=new string(map.Cast<char>().ToArray()))throw new Exception("Non deterministic");
			}
			for(selected=0;selected<4;selected++)
			{
				Start(777);enemies.Clear();var p=Directions.Select(d=>player+d).First(Walk);var e=new Enemy(p,'r',1){Hp=1,MaxHp=1};enemies.Add(e);rng=new FixedRandom(20);if(selected is 1 or 2)Shoot(p-player);else Move(p-player);
				if(enemies.Count!=0||xp!=EnemyXp('r',1))throw new Exception("Melee failed");
				hp=1;potions=2;Drink();if(hp!=21||potions!=1)throw new Exception("Potion failed");
				player=stairs;Descend();if(floor!=2)throw new Exception("Descent failed");
				foreach(int bossFloor in new[]{5,10,25,100})
				{
					floor=bossFloor;Generate();player=stairs;
					int previousHp=hp,previousEnergy=energy;Descend();
					if(floor!=bossFloor||hp!=previousHp||energy!=previousEnergy)throw new Exception("Living boss did not block descent");
					var boss=enemies.Single(e=>e.Glyph=='B');int previousPotions=potions;
					Hit(boss,boss.Hp);if(potions!=previousPotions+1)throw new Exception("Boss potion reward missing");
					Descend();if(floor!=bossFloor+1||screen!="game")throw new Exception("Endless descent failed");
				}
			}
			TestPresentation();TestBalance();TestEnemyAi();TestInventory();TestMerchant();TestCycleBalance();TestHeldMovement();
			GD.Print("SELF-TEST PASS: d20, criticals, 2d10 healing, attributes, scarce loot, scaling, costs, ranged restrictions, monster names, localization, menus, portraits, damage effects; 550 generated floors through depth 1000, reachability, occupancy, deterministic seeds, all classes, combat, healing, endless descent, boss gates and rewards; patrol, sight, pursuit, search, boss-room confinement.");GetTree().Quit();
		}
		catch(Exception e) {GD.PushError(e.ToString());GetTree().Quit(1);}
	}
}
