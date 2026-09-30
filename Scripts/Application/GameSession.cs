using Godot;
using System;
using System.Linq;
using static Abyss.Rules.TabletopRules;

namespace Abyss.Application;
internal sealed class GameSession : ITurnScheduler, IRunLifecycle
{
    internal IGameHost Host { get; }
    internal IAsciiCanvas Canvas { get; }
    internal ScreenTransitions Transitions { get; }
    internal OpeningStory OpeningStory { get; }
    internal ILanguageSettings Settings { get; }
    internal PlayerState PlayerState { get; } = new();
    internal DungeonState DungeonState { get; } = new();
    internal InventoryState InventoryState { get; } = new();
    internal RunState RunState { get; } = new();
    internal ExpeditionJournal ExpeditionJournal { get; } = new();
    internal MenuState MenuState { get; } = new();
    internal MerchantState MerchantState { get; } = new();
    internal RandomStream RandomStream { get; } = new();
    internal InventoryRenderer InventoryRenderer { get; }
    internal HudRenderer HudRenderer { get; }
    internal ActionEffectsRenderer ActionEffectsRenderer { get; }
    internal VisualEffects VisualEffects { get; }
    internal AsciiCanvas AsciiCanvas { get; }
    internal MerchantRenderer MerchantRenderer { get; }
    internal PauseRenderer PauseRenderer { get; }
    internal MerchantService MerchantService { get; }
    internal CombatService CombatService { get; }
    internal InventoryService InventoryService { get; }
    internal EnemyAi EnemyAi { get; }
    internal WardenAbilities WardenAbilities { get; }
    internal ProgressionService ProgressionService { get; }
    internal DungeonGenerator DungeonGenerator { get; }
    internal GameInput GameInput { get; }
    internal PlayerActions PlayerActions { get; }
    internal MenuController MenuController { get; }
    internal LootService LootService { get; }
    internal LanguagePreferences LanguagePreferences { get; }
    internal HeroCombatStats HeroCombatStats { get; }
    internal GameRenderer GameRenderer { get; }
    internal MenuRenderer MenuRenderer { get; }
    internal UiComponents UiComponents { get; }
    internal JournalFormatter JournalFormatter { get; }
    internal Localization Localization { get; }
    internal EnemyNavigator EnemyNavigator { get; }
    internal FloorEventGenerator FloorEventGenerator { get; }
    internal EnvironmentGenerator EnvironmentGenerator { get; }
    internal EnvironmentService EnvironmentService { get; }

    internal GameSession(IGameHost host, IAsciiCanvas canvas, ILanguageSettings settings)
    {
        Host = host;
        Transitions = new ScreenTransitions(canvas);
        Canvas = Transitions;
        Settings = settings;
        VisualEffects = new VisualEffects(DungeonState, PlayerState);
        MerchantService = new MerchantService(ExpeditionJournal, InventoryState, MenuState, MerchantState, PlayerState, RunState);
        LanguagePreferences = new LanguagePreferences(MenuState, Settings);
        HeroCombatStats = new HeroCombatStats(PlayerState, InventoryState);
        Localization = new Localization(MenuState);
        OpeningStory = new OpeningStory(RunState, Localization);
        AsciiCanvas = new AsciiCanvas(Canvas, VisualEffects);
        InventoryService = new InventoryService(this, ExpeditionJournal, InventoryState, Localization, MenuState, PlayerState, RunState, RandomStream);
        ProgressionService = new ProgressionService(ExpeditionJournal, HeroCombatStats, PlayerState);
        LootService = new LootService(DungeonState, ExpeditionJournal, InventoryState, Localization, RandomStream);
        JournalFormatter = new JournalFormatter(ExpeditionJournal, Localization);
        InventoryRenderer = new InventoryRenderer(AsciiCanvas, InventoryState, Localization, MenuState, PlayerState);
        ActionEffectsRenderer = new ActionEffectsRenderer(AsciiCanvas, DungeonState, VisualEffects.Actions);
        HudRenderer = new HudRenderer(AsciiCanvas, DungeonState, ExpeditionJournal, HeroCombatStats, InventoryState, Localization, PlayerState, RunState, VisualEffects, ActionEffectsRenderer);
        CombatService = new CombatService(DungeonState, ExpeditionJournal, HeroCombatStats, InventoryState, Localization, LootService, PlayerState, ProgressionService, RunState, VisualEffects, RandomStream);
        FloorEventGenerator = new FloorEventGenerator(DungeonState, PlayerState, RunState, ExpeditionJournal);
        EnvironmentGenerator = new EnvironmentGenerator(DungeonState, PlayerState, RunState);
        EnvironmentService = new EnvironmentService(DungeonState, InventoryState, PlayerState, RunState, ExpeditionJournal, CombatService, VisualEffects);
        DungeonGenerator = new DungeonGenerator(DungeonState, ExpeditionJournal, LootService, MenuState, MerchantState, PlayerState, VisualEffects, RandomStream, InventoryState, EnvironmentGenerator, FloorEventGenerator);
        UiComponents = new UiComponents(AsciiCanvas, Localization, MenuState);
        MerchantRenderer = new MerchantRenderer(AsciiCanvas, InventoryState, Localization, MenuState, MerchantService, PlayerState, UiComponents);
        PauseRenderer = new PauseRenderer(AsciiCanvas, DungeonState, ExpeditionJournal, HeroCombatStats, InventoryRenderer, InventoryState, JournalFormatter, Localization, MenuState, PlayerState, UiComponents);
        EnemyNavigator = new EnemyNavigator(DungeonState, PlayerState);
        WardenAbilities = new WardenAbilities(CombatService, DungeonState, PlayerState, ExpeditionJournal, VisualEffects);
        EnemyAi = new EnemyAi(new IEnemyBehavior[] { new WardenBehavior(CombatService, DungeonState, ExpeditionJournal, PlayerState, EnemyNavigator, WardenAbilities), new RoamingBehavior(CombatService, DungeonState, PlayerState, RandomStream, EnemyNavigator) });
        PlayerActions = new PlayerActions(CombatService, this, DungeonState, this, ExpeditionJournal, HeroCombatStats, InventoryState, LootService, MenuState, PlayerState, RunState, VisualEffects, RandomStream, EnvironmentService);
        MenuRenderer = new MenuRenderer(AsciiCanvas, DungeonState, Localization, MenuState, PlayerState, RunState, UiComponents);
        MenuController = new MenuController(Host, InventoryService, InventoryState, JournalFormatter, LanguagePreferences, MenuState, MerchantService, PlayerActions, PlayerState, RunState, this);
        GameRenderer = new GameRenderer(AsciiCanvas, HudRenderer, Localization, MenuRenderer, MerchantRenderer, PauseRenderer, RunState, MenuState, Transitions, OpeningStory);
        GameInput = new GameInput(this, ExpeditionJournal, Host, MenuController, MenuState, PlayerActions, PlayerState, RunState, VisualEffects, Transitions, OpeningStory);
    }

    internal Random RandomGenerator { get => RandomStream.Generator; set => RandomStream.Generator = value; }

    internal double Clock;
    public void Tick(double delta)
    {
        Clock += delta;
        Transitions.Advance(delta);
        OpeningStory.Advance(delta);
        VisualEffects.UiTime += delta;
        GameInput.AdvanceHeldMovement(delta);
        if (RunState.Screen == "game" || RunState.Screen == "dead")
            VisualEffects.AdvanceEffects(delta);
        if (Clock > (Transitions.Active || RunState.Screen == "intro" || (VisualEffects.Actions.Active && RunState.Screen == "game") ? 1.0 / 30 : .10))
        {
            Clock = 0;
            Host.RequestRedraw();
        }
    }

    internal void Start(int? fixedSeed = null)
    {
        DungeonState.Environment.Clear();
        RunState.Seed = fixedSeed ?? Random.Shared.Next(1, int.MaxValue);
        RandomGenerator = new Random(RunState.Seed);
        DungeonState.Floor = 1;
        PlayerState.Level = 1;
        PlayerState.Experience = PlayerState.Gold = RunState.Turn = PlayerState.Kills = 0;
        InventoryState.Potions = 5;
        PlayerState.Attributes = HeroAttributes(PlayerState.ClassIndex);
        PlayerState.MaxHealth = new[]
        {
            28,
            26,
            26,
            28
        }[PlayerState.ClassIndex] + Math.Max(0, PlayerState.Attributes.Con) * 4;
        PlayerState.Health = PlayerState.MaxHealth;
        PlayerState.MaxEnergy = new[]
        {
            9,
            14,
            10,
            9
        }[PlayerState.ClassIndex];
        PlayerState.Energy = PlayerState.MaxEnergy;
        ExpeditionJournal.LastRollPt = ExpeditionJournal.LastRollEn = "";
        ExpeditionJournal.LastPotionRoll = ExpeditionJournal.LastPotionHealing = 0;
        RunState.Screen = "game";
        RunState.IsAiming = false;
        ExpeditionJournal.Entries.Clear();
        InventoryService.ResetInventory();
        VisualEffects.ResetEffects();
        DungeonGenerator.Generate();
        ExpeditionJournal.Say("Desca o mais longe possivel.", "Descend as far as you can.");
        ExpeditionJournal.Say("Voce entrou no Abismo. Cada passo conta.", "You entered the Abyss. Every step counts.");
    }

    internal void Descend()
    {
        if (PlayerState.Position != DungeonState.Stairs)
        {
            ExpeditionJournal.Say("Procure a escada [>] e pise nela.", "Find the stairs [>] and stand on them.");
            return;
        }

        if (GameRules.IsBossFloor(DungeonState.Floor) && DungeonState.Enemies.Any(e => e.Glyph == 'B'))
        {
            ExpeditionJournal.Say("O Guardiao ainda bloqueia a descida.", "The Warden still blocks the descent.");
            return;
        }

        DungeonState.Floor++;
        PlayerState.Health = Math.Min(PlayerState.MaxHealth, PlayerState.Health + 2);
        PlayerState.Energy = Math.Min(PlayerState.MaxEnergy, PlayerState.Energy + 3);
        DungeonGenerator.Generate();
        ExpeditionJournal.Say($"Andar {DungeonState.Floor}. +2 PV, +3 energia.", $"Floor {DungeonState.Floor}. +2 HP, +3 energy.");
    }

    internal void EndTurn(bool evade = false, Vector2I? previousPlayer = null)
    {
        RunState.Turn++;
        DungeonGenerator.Reveal();
        foreach (Enemy e in DungeonState.Enemies.ToArray())
        {
            EnemyAi.ActEnemy(e, evade, !previousPlayer.HasValue || previousPlayer.Value == PlayerState.Position || GameRules.Dist(previousPlayer.Value, e.Position) == 1);
            if (PlayerState.Health <= 0)
                break;
        }

        if (PlayerState.Health > 0) EnvironmentService.Tick();
        if (RunState.Turn % (DungeonState.Modifier == FloorModifier.ThinAir ? 12 : 6) == 0)
            PlayerState.Energy = Math.Min(PlayerState.MaxEnergy, PlayerState.Energy + 1);
        DungeonGenerator.Reveal();
    }

    void ITurnScheduler.EndTurn(bool evade, Vector2I? previousPlayer) => EndTurn(evade, previousPlayer);
    void IRunLifecycle.Start(int? fixedSeed) => Start(fixedSeed);
    void IRunLifecycle.Descend() => Descend();
}
