using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using static TabletopRules;

public partial class Main
{
	bool english, testing;
	int menuIndex, languageIndex;
	string languageReturn="home";
	string languageNotice="";
	double uiTime, heroHurt, focusHold;
	int heroDamage;
	Enemy? focus;
	const double HurtDuration=.95;
	const float MapX=272, MapY=145, CellX=11, CellY=16.5f;
	static readonly string[] EnglishNames={"WARRIOR","MAGE","ARCHER","ROGUE"};
	static readonly string[] EnglishSkills={"Whirlwind","Arcane nova","Piercing arrow","Shadow step"};
	readonly List<DamageEffect> effects=new();
	sealed class DamageEffect
	{
		public Vector2I Position;
		public int Damage;
		public bool Hero;
		public double Remaining=HurtDuration;
	}
	string T(string pt,string en)=>english?en:pt;
	string ClassName(int i)=>english?EnglishNames[i]:names[i];
	string SkillName(int i)=>english?EnglishSkills[i]:skills[i];
	string EnemyName(char g)=>g switch {'r'=>T("RATO DAS CRIPTAS","CRYPT RAT"),'s'=>T("ESQUELETO","SKELETON"),'g'=>T("GOBLIN","GOBLIN"),'B'=>T("GUARDIAO","WARDEN"),_=>T("SEM ALVO","NO TARGET")};
	const string SettingsPath="user://settings.cfg";
	void LoadLanguage(string path=SettingsPath)
	{
		var config=new ConfigFile();
		if(config.Load(path)==Error.Ok) english=config.GetValue("display","language","pt").AsString()=="en";
	}
	void SaveLanguage(string path=SettingsPath)
	{
		if(testing && path==SettingsPath)return;
		var config=new ConfigFile();config.Load(path);
		config.SetValue("display","language",english?"en":"pt");
		languageNotice=config.Save(path)==Error.Ok?"":"save_error";
	}
	void OpenLanguage(string from) {languageReturn=from;languageIndex=english?1:0;screen="language";}
	static bool Previous(Key key)=>key==Key.Up||key==Key.W;
	static bool Next(Key key)=>key==Key.Down||key==Key.S;
	static bool Confirm(Key key)=>key==Key.Enter||key==Key.Space;
	bool HandleMenus(Key key)
	{
		if(screen=="game")return false;
		if(screen=="home")
		{
			if(Previous(key))menuIndex=(menuIndex+2)%3;
			if(Next(key))menuIndex=(menuIndex+1)%3;
			if(key==Key.Key1)menuIndex=0;
			if(key==Key.Key2)menuIndex=1;
			if(key==Key.Key3)menuIndex=2;
			if(Confirm(key))
			{
				if(menuIndex==0)screen="classes";
				else if(menuIndex==1)OpenLanguage("home");
				else GetTree().Quit();
			}
		}
		else if(screen=="language")
		{
			if(Previous(key)||Next(key)||key==Key.Left||key==Key.Right)languageIndex=1-languageIndex;
			if(key==Key.Key1)languageIndex=0;
			if(key==Key.Key2)languageIndex=1;
			if(Confirm(key)) {english=languageIndex==1;SaveLanguage();screen=languageReturn;menuIndex=0;}
			if(key==Key.Escape) {screen=languageReturn;menuIndex=0;}
		}
		else if(screen=="classes")
		{
			if(key>=Key.Key1&&key<=Key.Key4)selected=(int)key-(int)Key.Key1;
			if(key==Key.Left||key==Key.A)selected=(selected+3)%4;
			if(key==Key.Right||key==Key.D)selected=(selected+1)%4;
			if(Confirm(key))Start();
			if(key==Key.Escape){screen="home";menuIndex=0;}
		}
		else if(screen=="pause") HandlePause(key);
		else if(screen=="shop")HandleShop(key);
		else if(screen=="confirm_exit")HandleExitConfirm(key);
		else if(screen=="dead")
		{if(Confirm(key)){screen="home";menuIndex=0;}}
		return true;
	}
	void ResetEffects() {effects.Clear();heroHurt=focusHold=0;heroDamage=0;focus=null;}
	void EnemyHurt(Enemy e,int damage)
	{
		e.Hurt=HurtDuration;e.LastDamage=damage;focus=e;focusHold=HurtDuration;
		effects.Add(new DamageEffect{Position=e.P,Damage=damage});
	}
	void HeroHurt(Enemy source,int damage)
	{
		heroHurt=HurtDuration;heroDamage=damage;
		if(focusHold<=0){focus=source;focusHold=HurtDuration;}
		effects.Add(new DamageEffect{Position=player,Damage=damage,Hero=true});
	}
	void AdvanceEffects(double delta)
	{
		heroHurt=Math.Max(0,heroHurt-delta);focusHold=Math.Max(0,focusHold-delta);
		foreach(var e in enemies)e.Hurt=Math.Max(0,e.Hurt-delta);
		if(focus!=null&&!enemies.Contains(focus))focus.Hurt=Math.Max(0,focus.Hurt-delta);
		foreach(var fx in effects)fx.Remaining-=delta;
		effects.RemoveAll(fx=>fx.Remaining<=0);
	}
	Enemy? FocusEnemy()
	{
		if(focus!=null && ((focus.Hp<=0&&focusHold>0)||(enemies.Contains(focus)&&visible[focus.P.X,focus.P.Y])))return focus;
		focus=enemies.Where(e=>visible[e.P.X,e.P.Y]).OrderBy(e=>Dist(e.P,player)).FirstOrDefault();
		return focus;
	}
	void CycleTarget()
	{
		var targets=enemies.Where(e=>visible[e.P.X,e.P.Y]).OrderBy(e=>Dist(e.P,player)).ToList();
		if(targets.Count==0){focus=null;return;}
		int i=focus==null?-1:targets.IndexOf(focus);focus=targets[(i+1)%targets.Count];focusHold=0;
	}
	void Frame(float x,float y,int columns,int rows,Color color,int size=15,int step=18)
	{
		Text(x,y,"+"+new string('-',columns-2)+"+",color,size);
		for(int r=1;r<rows-1;r++)Text(x,y+r*step,"|"+new string(' ',columns-2)+"|",color,size);
		Text(x,y+(rows-1)*step,"+"+new string('-',columns-2)+"+",color,size);
	}
	void Art(float x,float y,string art,Color color,int size=17,int spacing=19)
	{
		float extent=art==AsciiArt.Book?300:art==AsciiArt.Crown||art==AsciiArt.Grave?440:510;
		DrawAsciiImage(x,y-30,art,extent,extent,new Color("fff6df"),art==AsciiArt.Tower?1:0);
	}
	sealed class GlyphLayer
	{
		public int Row,Tone,Column;
		public string Text="";
	}
	sealed class GlyphPicture
	{
		public int Columns,Rows;
		public readonly System.Collections.Generic.List<GlyphLayer> Layers=new();
		public readonly List<GlyphLayer> LightLayers=new();
	}
	readonly System.Collections.Generic.Dictionary<string,GlyphPicture> imageCache=new();
	void DrawAsciiImage(float x,float y,string art,float width,float height,Color tint,int illumination=0)
	{
		if(!imageCache.TryGetValue(art,out var picture))
		{
			var rows=art.Split('\n');
			var tones=AsciiArt.ToneMaps[art].Split('\n');
			picture=new GlyphPicture{Columns=rows.Max(r=>r.Length),Rows=rows.Length};
			for(int row=0;row<rows.Length;row++)for(int tone=0;tone<8;tone++)
			{
				var line=new char[rows[row].Length];Array.Fill(line,' ');bool any=false;
				for(int col=0;col<line.Length;col++)if(tones[row][col]-'0'==tone&&rows[row][col]!=' '){line[col]=rows[row][col];any=true;}
				if(any)picture.Layers.Add(new GlyphLayer{Row=row,Tone=tone,Text=new string(line)});
			}
			if(art==AsciiArt.Tower||art==AsciiArt.Camp||art==AsciiArt.Book)
				foreach(var layer in picture.Layers)
					for(int col=0;col<layer.Text.Length;col+=16)
						picture.LightLayers.Add(new GlyphLayer{Row=layer.Row,Tone=layer.Tone,Column=col,Text=layer.Text.Substring(col,Math.Min(16,layer.Text.Length-col))});
			imageCache[art]=picture;
		}
		float cell=font.GetStringSize(new string('M',100),HorizontalAlignment.Left,-1,10).X/100;
		float scale=Math.Min(width/(picture.Columns*cell),height/(picture.Rows*10));
		DrawSetTransform(new Vector2(x,y),0,new Vector2(scale,scale));
		foreach(var layer in illumination==0?picture.Layers:picture.LightLayers)
		{
			float light=.22f+.78f*layer.Tone/7f;
			float glow=illumination==0?1:AsciiGlow(illumination,(layer.Column+8f)/picture.Columns,(float)layer.Row/picture.Rows,uiTime);
			DrawString(font,new Vector2(layer.Column*cell,9+layer.Row*10),layer.Text,HorizontalAlignment.Left,-1,10,new Color(Math.Min(1,tint.R*light*glow),Math.Min(1,tint.G*light*glow),Math.Min(1,tint.B*light*glow),1));
		}
		DrawSetTransform(Vector2.Zero,0,Vector2.One);
	}
	void Portrait(float x,float y,string art,string title,Color color,double hurt=0,int damage=0)
	{
		int stage=(int)((HurtDuration-hurt)*14);
		bool impact=hurt>0;
		Color tint=impact?(stage%2==0?red:gold):color;
		Frame(x,y,25,17,tint);
		Text(x+18,y+24,title,tint,14);
		string face=art;
		float shake=impact?(stage%2==0?-3:3):0;
		DrawAsciiImage(x+9+shake,y+35,face,207,212,impact?tint:new Color("fff6df"));
		if(impact)
		{
			string burst=stage%3==0?"*  /  !  \\  *":stage%3==1?"+  *  #  *  +":".  +  *  +  .";
			Text(x+24,y+258,burst,red,17);
			Text(x+57,y+279,$"-{damage} HP",red,18);
		}
		else Text(x+22,y+278,"<------ + ------>",dim,15);
	}
	public override void _Draw()
	{
		if(font==null)return;
		Text(32,34,T("A B I S M O","A B Y S S"),gold,22);
		Rule(54);
		switch(screen)
		{
			case "home":DrawHome();return;
			case "language":DrawLanguage();return;
			case "classes":DrawClasses();return;
			case "pause":DrawPause();return;
			case "shop":DrawShop();return;
			case "confirm_exit":DrawExitConfirm();return;
			case "dead":DrawEnd();return;
		}
		DrawGame();
	}
	void MenuItem(float x,float y,int index,string label)
	{Text(x,y,(menuIndex==index?"> [ ":"  [ ")+label+" ]",menuIndex==index?teal:dim,23);}
	void Footer(string pt,string en)
	{Rule(709);Text(32,748,T(pt,en),teal,17);if(languageNotice!="")Text(32,781,T("Nao foi possivel salvar o idioma.","Could not save the language preference."),red,14);}
	void DrawHome()
	{
		Text(32,99,T("A PORTA DO ABISMO","THE GATE TO THE ABYSS"),teal,16);
		Art(32,169,AsciiArt.Tower,dim);
		Text(655,210,T("A luz termina aqui.","The light ends here."),gold,30);
		MenuItem(655,414,0,T("INICIAR JOGO","START GAME"));
		MenuItem(655,473,1,T("IDIOMA","LANGUAGE"));
		MenuItem(655,532,2,T("SAIR DO JOGO","QUIT GAME"));
		Footer("[SETAS / W S] selecionar   [ENTER] confirmar   [F11] tela cheia","[ARROWS / W S] select   [ENTER] confirm   [F11] fullscreen");
	}
	void DrawLanguage()
	{
		Text(32,99,T("02 / IDIOMA","02 / LANGUAGE"),teal,16);
		Art(40,181,AsciiArt.Globe,dim);
		Text(650,210,"PORTUGUES / ENGLISH",gold,26);
		Lines(650,269,T("Escolha o idioma da sua aventura.\nA preferencia sera salva neste computador.","Choose the language of your adventure.\nYour preference is saved on this computer."),ink,17,29);
		Text(650,401,(languageIndex==0?"> ":"  ")+"[1] Portugues (Brasil)",languageIndex==0?teal:dim,22);
		Text(650,461,(languageIndex==1?"> ":"  ")+"[2] English",languageIndex==1?teal:dim,22);
		Footer("[SETAS / 1-2] selecionar   [ENTER] aplicar   [ESC] voltar","[ARROWS / 1-2] select   [ENTER] apply   [ESC] back");
	}
	void DrawClasses()
	{
		Text(32,99,T("ESCOLHA QUEM VAI DESCER","CHOOSE WHO WILL DESCEND"),teal,16);
		Text(32,147,T("Quatro destinos. Uma unica saida.","Four destinies. One way out."),gold,27);
		string[] pt={"Resiste e luta de perto.","Controla grupos com magia.","Acerta de longa distancia.","Golpe forte e evasao."};
		string[] en={"Armored close combat.","Controls groups with magic.","Strikes from a distance.","Heavy strikes and evasion."};
		for(int i=0;i<4;i++)
		{
			float x=32+i*309;Color color=i==selected?teal:dim;
			Portrait(x+26,195,AsciiArt.Heroes[i],$"[{i+1}] {ClassName(i)}",color);
			float portraitWidth=font.GetStringSize(new string('-',25),HorizontalAlignment.Left,-1,15).X;
			string description=T(pt[i],en[i]);
			float descriptionWidth=font.GetStringSize(description,HorizontalAlignment.Left,-1,15).X;
			Text(x+26+(portraitWidth-descriptionWidth)/2,528,description,ink,15);
			string selection=i==selected?T("> HEROI SELECIONADO <","> HERO SELECTED <"):". . .";
			float selectionWidth=font.GetStringSize(selection,HorizontalAlignment.Left,-1,15).X;
			Text(x+26+(portraitWidth-selectionWidth)/2,576,selection,color,15);
		}
		Footer("[1-4 / A D] classe   [ENTER] iniciar expedicao   [ESC] voltar","[1-4 / A D] class   [ENTER] start expedition   [ESC] back");
	}

	void DrawEnd()
	{
		Text(32,99,T("O ABISMO GUARDA SUA HISTORIA","THE ABYSS KEEPS YOUR STORY"),teal,16);
		Art(32,205,AsciiArt.Grave,dim);
		Text(548,195,T("SUA EXPEDICAO TERMINOU","YOUR EXPEDITION HAS ENDED"),red,25);
		Text(548,235,T($"ANDAR ALCANCADO: {floor}",$"FLOOR REACHED: {floor}"),gold,22);
		Text(548,266,$"{ClassName(selected)} / {T("NIVEL","LEVEL")} {level}",teal,17);
		Portrait(548,285,AsciiArt.Heroes[selected],ClassName(selected),teal);
		Lines(827,352,T($"{kills} inimigos derrotados\n{coins} moedas coletadas\n{turn} turnos",$"{kills} enemies defeated\n{coins} gold collected\n{turn} turns"),ink,17,31);
		Footer("[ENTER] voltar ao menu inicial","[ENTER] return to the main menu");
	}

	void DrawGame()
	{
		Text(32,91,T($"ANDAR {floor}",$"FLOOR {floor}"),gold,18);
		Text(1012,91,merchantFloor?T("MERCADOR","MERCHANT"):T("ALVO SELECIONADO","SELECTED TARGET"),merchantFloor?gold:red,16);
		var target=FocusEnemy();
		Portrait(24,121,AsciiArt.Heroes[selected],"@ "+ClassName(selected),teal,heroHurt,heroDamage);
		Portrait(1012,121,merchantFloor?AsciiArt.Merchant:AsciiArt.Enemy(target?.Glyph??'?'),merchantFloor?T("MERCADOR","MERCHANT"):EnemyName(target?.Glyph??'?'),merchantFloor?gold:target==null?dim:red,target?.Hurt??0,target?.LastDamage??0);
		Text(32,432,T($"VIDA {hp}/{maxHp}",$"HEALTH {hp}/{maxHp}"),red,15);Text(32,453,Bar(hp,maxHp),red,16);
		Text(32,481,T($"ENERGIA {energy}/{maxEnergy}",$"ENERGY {energy}/{maxEnergy}"),teal,15);Text(32,502,Bar(energy,maxEnergy),teal,16);
		Text(32,543,T($"NV {level}  XP {xp}/{XpToNext}",$"LV {level}  XP {xp}/{XpToNext}"),gold,15);
		Text(32,575,T($"POCOES {potions}  OURO {coins}",$"POTIONS {potions}  GOLD {coins}"),ink,14);
		if(target!=null)
		{
			Text(1019,432,target.Hp<=0?T("DERROTADO","DEFEATED"):T($"VIDA {target.Hp}/{target.MaxHp}",$"HEALTH {target.Hp}/{target.MaxHp}"),red,15);
			Text(1019,453,Bar(Math.Max(0,target.Hp),target.MaxHp),red,16);
		}

		DrawJournalSummary();

		Text(272,121,aiming?T("> MIRA: WASD / SETAS. ESC cancela.","> AIM: WASD / ARROWS. ESC cancels."):merchantFloor?T("[E] Converse ao lado de M. [>] Continue sua jornada.","[E] Talk next to M. [>] Continue your journey."):T("[>] Encontre a passagem para as profundezas.","[>] Find the passage into the depths."),aiming?gold:teal,15);
		for(int y=0;y<H;y++)for(int x=0;x<W;x++)
		{
			if(!seen[x,y])continue;
			char g=map[x,y];Color c=visible[x,y]?(g=='#'?new Color("71879a"):new Color("354452")):new Color("23303e");
			var pos=new Vector2I(x,y);
			if(g=='>')c=visible[x,y]?gold:dim;
			if(visible[x,y])
			{
				if(items.TryGetValue(pos,out char item)){g=item;c=item=='!'?red:item=='*'?teal:gold;}
				var e=At(pos);if(e!=null){g=e.Glyph;c=e==target?gold:red;if(e.Hurt>0)g=ImpactGlyph(e.Hurt);}
				if(merchantFloor&&pos==merchantPosition){g='M';c=gold;}
				if(pos==player){g=heroHurt>0?ImpactGlyph(heroHurt):'@';c=heroHurt>0?red:teal;}
			}
			Text(MapX+x*CellX,MapY+y*CellY,g.ToString(),c,17);
		}
		foreach(var fx in effects)
		{
			float rise=(float)((HurtDuration-fx.Remaining)*25);
			Text(MapX+fx.Position.X*CellX-4,MapY+fx.Position.Y*CellY-16-rise,$"-{fx.Damage}",fx.Hero?red:gold,15);
		}
		// Ability details have their own panel below the map, away from enemy stats.
		Text(272,597,T("SUAS ACOES / CUSTO / DADOS DE DANO","YOUR ACTIONS / COST / DAMAGE DICE"),gold,13);
		Frame(270,612,78,4,dim,15,18);
		Text(283,634,$"[Q] {SkillName(selected)}  |  {AbilityCost} EN  |  {AbilityDice}",energy>=AbilityCost?teal:dim,15);
		Text(283,655,CanShoot?$"[F] {T("Basico","Basic")}  |  {ShotCost} EN  |  {ShotDice}":!CanMelee?T("[F] Equipe um arco/cajado compativel.","[F] Equip a compatible bow/staff."):T($"[Mover contra inimigo] Ataque basico: {MeleeDice}",$"[Bump into enemy] Basic attack: {MeleeDice}"),ink,15);
		Rule(685);
		Text(32,707,T("WASD mover | Q habilidade | P pocao | E interagir | ESPACO esperar | TAB trocar alvo | ESC pausa","WASD move | Q ability | P potion | E interact | SPACE wait | TAB switch target | ESC pause"),ink,15);
		Text(32,779,T("@ voce   # parede   > escada   ! pocao   $ ouro   * cristal   C bau   M mercador","@ you   # wall   > stairs   ! potion   $ gold   * crystal   C chest   M merchant"),dim,13);
	}
	void DrawJournalSummary()
	{
		Frame(1012,478,25,12,dim);
		Text(1026,501,T("DIARIO / ROLAGENS","JOURNAL / ROLLS"),gold,12);
		float y=524;
		if(log.Count==0)Text(1026,y,T("Sem registros.","No entries yet."),dim,12);
		for(int i=0;i<Math.Min(2,log.Count);i++)
		{
			string remaining="> "+T(log[i].Pt,log[i].En);
			for(int line=0;line<4&&remaining.Length>0;line++)
			{
				string text;
				if(remaining.Length<=28){text=remaining;remaining="";}
				else if(line==3){text=remaining[..25]+"...";remaining="";}
				else
				{
					int cut=remaining.LastIndexOf(' ',27);
					if(cut<3)cut=28;
					text=remaining[..cut];remaining="  "+remaining[cut..].TrimStart();
				}
				Text(1026,y,text,i==0?ink:dim,12);y+=15;
			}
			y+=6;
		}
	}
	static char ImpactGlyph(double remaining)=>((int)((HurtDuration-remaining)*15)%4) switch {0=>'*',1=>'#',2=>'!',_=>'+'};
	void TestPresentation()
	{
		testing=true;english=false;selected=0;screen="home";menuIndex=0;
		HandleKey(Key.Down);HandleKey(Key.Enter);
		if(screen!="language")throw new Exception("Home language navigation failed");
		HandleKey(Key.Key2);HandleKey(Key.Enter);
		if(!english||screen!="home"||ClassName(0)!="WARRIOR")throw new Exception("English selection failed");
		HandleKey(Key.Enter);if(screen!="classes")throw new Exception("Class screen failed");
		HandleKey(Key.Key4);HandleKey(Key.Enter);
		if(screen!="game"||selected!=3||!log[0].En.StartsWith("You entered"))throw new Exception("Localized start failed");
		int before=turn;HandleKey(Key.Escape);HandleKey(Key.Key5);HandleKey(Key.Enter);
		HandleKey(Key.Key1);HandleKey(Key.Enter);HandleKey(Key.Escape);
		if(english||screen!="game"||turn!=before)throw new Exception("Pause language switching changed game state");
		screen="language";languageReturn="home";HandleKey(Key.Key2);HandleKey(Key.Escape);
		if(english)throw new Exception("Cancelled language selection was applied");
		screen="game";HandleKey(Key.Escape);
		var position=player;int savedHp=hp,savedEnergy=energy,savedPotions=potions;
		HandleKey(Key.Q);HandleKey(Key.P);HandleKey(Key.Space);HandleKey(Key.W);
		if(turn!=before||player!=position||hp!=savedHp||energy!=savedEnergy||potions!=savedPotions)throw new Exception("Paused actions changed the expedition");
		for(int i=0;i<45;i++)Say($"Registro {i}: "+new string('x',100),$"Entry {i}: "+new string('x',100));
		HandleKey(Key.Key3);HandleKey(Key.Down);
		if(pauseTab!=2||journalPage!=1||log.Count<45||JournalLines().Any(line=>line.Length>91))throw new Exception("Journal history or pagination failed");
		HandleKey(Key.End);HandleKey(Key.Down);
		if(journalPage!=(JournalLines().Count-1)/JournalPageSize)throw new Exception("Journal last-page boundary failed");
		HandleKey(Key.Home);HandleKey(Key.Up);
		if(journalPage!=0)throw new Exception("Journal first-page boundary failed");
		HandleKey(Key.Key5);HandleKey(Key.Enter);HandleKey(Key.Escape);
		if(screen!="pause"||pauseTab!=4)throw new Exception("Settings return tab lost");
		HandleKey(Key.Down);HandleKey(Key.Enter);
		if(screen!="confirm_exit")throw new Exception("Missing exit confirmation");
		HandleKey(Key.Down);HandleKey(Key.Enter);
		if(screen!="home"||turn!=before)throw new Exception("Pause main-menu action failed");
		screen="game";selected=0;Start(17);enemies.Clear();
		var pos=Directions.Select(d=>player+d).First(Walk);
		var foe=new Enemy(pos,'g',1){Hp=100,MaxHp=100};enemies.Add(foe);Reveal();rng=new FixedRandom(20);
		Hit(foe,7);EndTurn();
		if(heroHurt<=0||foe.Hurt<=0||effects.Count!=2||FocusEnemy()!=foe)throw new Exception("Damage effects missing");
		var glyph=ImpactGlyph(foe.Hurt);AdvanceEffects(.2);
		if(ImpactGlyph(foe.Hurt)==glyph)throw new Exception("Damage characters did not animate");
		before=turn;AdvanceEffects(2);
		if(effects.Count!=0||heroHurt!=0||foe.Hurt!=0||turn!=before)throw new Exception("Effects changed turns or failed to expire");
		Hit(foe,1000);if(FocusEnemy()!=foe)throw new Exception("Killing blow portrait vanished too early");
		AdvanceEffects(2);if(FocusEnemy()!=null)throw new Exception("Stale dead target");
		var arts=AsciiArt.Heroes.Concat(new[]{AsciiArt.Merchant,AsciiArt.Rat,AsciiArt.Skeleton,AsciiArt.Goblin,AsciiArt.Warden,AsciiArt.Unknown,AsciiArt.Tower,AsciiArt.Camp,AsciiArt.Globe,AsciiArt.Book,AsciiArt.Grave,AsciiArt.Crown});
		if(arts.Any(a=>a.Any(c=>c!='\n'&&(c<32||c>126))))throw new Exception("Non-ASCII art");
		if(AsciiArt.Heroes.Distinct().Count()!=4)throw new Exception("Portraits not unique");
		var testPath="user://language-test-"+Guid.NewGuid().ToString("N")+".cfg";
		try
		{
			english=true;SaveLanguage(testPath);english=false;LoadLanguage(testPath);
			if(!english)throw new Exception("English preference not persisted");
			english=false;SaveLanguage(testPath);english=true;LoadLanguage(testPath);
			if(english)throw new Exception("Portuguese preference not persisted");
		}
		finally {DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(testPath));}
		screen="game";int helpTurn=turn;HandleKey(Key.H);
		if(screen!="game"||turn!=helpTurn)throw new Exception("Removed H shortcut still acts");
		HandleKey(Key.Escape);HandleKey(Key.Key4);
		if(pauseTab!=3)throw new Exception("Help tab order failed");
		for(int topic=0;topic<7;topic++)
		{
			helpTopic=topic;
			foreach(bool language in new[]{false,true})
			{
				english=language;
				if(HelpText(topic).Split('\n').Any(line=>line.Length>89))throw new Exception("Help topic exceeds panel width");
			}
		}
		helpTopic=0;HandleKey(Key.Down);if(helpTopic!=1||turn!=helpTurn)throw new Exception("Help navigation changed game state");
		HandleKey(Key.Key5);if(pauseTab!=4)throw new Exception("Settings tab order failed");
		for(int mode=1;mode<=3;mode++)
		{
			if(AsciiGlow(mode,.5f,.5f,0)==AsciiGlow(mode,.5f,.5f,1))throw new Exception("ASCII light does not animate");
			for(int t=0;t<100;t++)if(AsciiGlow(mode,.5f,.5f,t/10.0)<.7||AsciiGlow(mode,.5f,.5f,t/10.0)>1.3)throw new Exception("ASCII light exceeds subtle brightness bounds");
		}
		testing=false;
	}
}
