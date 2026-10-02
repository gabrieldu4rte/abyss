using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using static Abyss.Rules.TabletopRules;

namespace Abyss.Application;
internal sealed class PlayerActions
{
    private readonly HeroVitals vitals;
    private readonly EnvironmentService environment;
    private readonly CombatService combatService;
    private readonly IRunLifecycle lifecycle;
    private readonly DungeonState dungeonState;
    private readonly ITurnScheduler turns;
    private readonly ExpeditionJournal expeditionJournal;
    private readonly HeroCombatStats heroCombatStats;
    private readonly InventoryState inventoryState;
    private readonly LootService lootService;
    private readonly MenuState menuState;
    private readonly PlayerState playerState;
    private readonly RunState runState;
    private readonly VisualEffects visualEffects;
    private readonly RandomStream random;
    internal PlayerActions(CombatService combatService, IRunLifecycle lifecycle, DungeonState dungeonState, ITurnScheduler turns, ExpeditionJournal expeditionJournal, HeroCombatStats heroCombatStats, InventoryState inventoryState, LootService lootService, MenuState menuState, PlayerState playerState, RunState runState, VisualEffects visualEffects, RandomStream random, EnvironmentService environment, HeroVitals vitals)
    {
        this.combatService = combatService;
        this.environment = environment;
        this.vitals = vitals;
        this.lifecycle = lifecycle;
        this.dungeonState = dungeonState;
        this.turns = turns;
        this.expeditionJournal = expeditionJournal;
        this.heroCombatStats = heroCombatStats;
        this.inventoryState = inventoryState;
        this.lootService = lootService;
        this.menuState = menuState;
        this.playerState = playerState;
        this.runState = runState;
        this.visualEffects = visualEffects;
        this.random = random;
    }

    internal void ToggleTorch()
    {
        if (inventoryState.TorchFuel == 0)
        {
            if (inventoryState.SpareTorches == 0)
            {
                menuState.InventoryNotice = ("Sem tochas de reserva.", "No spare torches.");
                return;
            }
            inventoryState.LightReserve();
        }
        else inventoryState.TorchEquipped = !inventoryState.TorchEquipped;
        runState.Screen = "game";
        expeditionJournal.Say(inventoryState.HasLight ? "Tocha acesa." : "Tocha guardada.", inventoryState.HasLight ? "Torch lit." : "Torch stowed.");
        turns.EndTurn();
    }
    internal void BeginTorchThrow()
    {
        if (runState.Screen != "pause" || menuState.PauseTab != 1) return;
        if (!inventoryState.HasLight && inventoryState.SpareTorches == 0)
        {
            menuState.InventoryNotice = ("Sem tocha acesa ou reserva.", "No lit torch or spare available.");
            return;
        }
        runState.IsAiming = false;
        runState.Screen = "torch_aim";
    }
    internal void ThrowTorch(Vector2I direction)
    {
        if (runState.Screen != "torch_aim") return;
        if (!environment.ThrowTorch(direction)) return;
        runState.Screen = "game";
        turns.EndTurn();
    }

    internal void BeginAim()
    {
        if (!heroCombatStats.CanShoot)
        {
            runState.IsAiming = false;
            expeditionJournal.Say("Equipe um cajado ou arco compativel para disparar.", "Equip a compatible staff or bow to shoot.");
            return;
        }

        if (runState.IsAiming)
        {
            runState.IsAiming = false;
            return;
        }

        runState.IsAiming = true;
        expeditionJournal.Say($"Ataque basico: escolha a direcao. Sem custo de energia.", $"Basic attack: choose a direction. No energy cost.");
    }

    internal void Interact()
    {
        if (dungeonState.BlacksmithRoom.HasValue && GameRules.Dist(playerState.Position, dungeonState.BlacksmithPosition) == 1)
        {
            runState.Screen = "blacksmith";
            menuState.BlacksmithIndex = 0;
            menuState.PendingUpgrade = null;
            menuState.ShopNotice = ("", "");
            return;
        }
        if (dungeonState.IsMerchantFloor && GameRules.Dist(playerState.Position, dungeonState.MerchantPosition) == 1)
        {
            runState.Screen = "shop";
            menuState.ShopSelling = false;
            menuState.ShopIndex = 0;
            menuState.PendingTrade = null;
            menuState.ShopNotice = ("", "");
            return;
        }

        lifecycle.Descend();
    }

    internal void Move(Vector2I d)
    {
        var previousPlayer = playerState.Position;
        var p = playerState.Position + d;
        if (!dungeonState.Walk(p))
            return;
        if (dungeonState.IsMerchantFloor && p == dungeonState.MerchantPosition)
        {
            expeditionJournal.Say("[E] Conversar com o mercador.", "[E] Talk to the merchant.");
            return;
        }

        if (dungeonState.BlacksmithRoom.HasValue && p == dungeonState.BlacksmithPosition)
        {
            expeditionJournal.Say("[E] Conversar com o Ferreiro Perdido.", "[E] Talk to the Lost Blacksmith.");
            return;
        }
        if (environment.Strike(p)) { turns.EndTurn(); return; }
        var enemy = dungeonState.At(p);
        if (enemy != null)
        {
            if (!heroCombatStats.CanMelee)
            {
                expeditionJournal.Say("Use [F] para disparar ou [Q] para a habilidade.", "Use [F] to shoot or [Q] for your ability.");
                return;
            }

            combatService.ResolveHeroAttack(enemy);
        }
        else
        {
            visualEffects.Sounds.Play("step");
            playerState.Position = p;
            Pickup();
        }

        turns.EndTurn(previousPlayer: previousPlayer);
    }

    internal void Pickup()
    {
        if (!dungeonState.Items.Remove(playerState.Position, out char g))
            return;
        visualEffects.Sounds.Play(g == 'C' ? "chest" : "pickup");
        if (g == 't')
        {
            inventoryState.SpareTorches++;
            expeditionJournal.Say("Tocha recolhida.", "Torch collected.");
            return;
        }
        if (g == 'C')
        {
            lootService.OpenChest();
            return;
        }

        if (g == '$')
        {
            int n = random.Generator.Next(3, 9);
            playerState.Gold += n;
            expeditionJournal.Say($"Tesouro: +{n} moedas.", $"Treasure: +{n} gold.");
        }

        if (g == '!')
        {
            inventoryState.Potions++;
            expeditionJournal.Say("Pocao encontrada. [P] para beber.", "Potion found. Press [P] to drink.");
        }

        if (g == '*')
        {
            playerState.Energy = Math.Min(playerState.MaxEnergy, playerState.Energy + 5);
            expeditionJournal.Say("Cristal: +5 energia.", "Crystal: +5 energy.");
        }
    }

    internal void Shoot(Vector2I d)
    {
        if (!heroCombatStats.CanShoot)
        {
            expeditionJournal.Say("Equipe um cajado ou arco compativel para disparar.", "Equip a compatible staff or bow to shoot.");
            return;
        }

        int range = playerState.ClassIndex == 2 ? 10 : 6;
        var origin = playerState.Position;
        var path = new List<Vector2I> { origin };
        var p = origin;
        for (int i = 0; i < range; i++)
        {
            p += d;
            if (!dungeonState.Walk(p))
                break;
            path.Add(p);
            environment.ReactElement(p);
            if (inventoryState.Weapon?.Special == ItemId.FluidStaff) environment.Wet(p);
            if (dungeonState.BlacksmithRoom.HasValue && p == dungeonState.BlacksmithPosition)
        {
            expeditionJournal.Say("[E] Conversar com o Ferreiro Perdido.", "[E] Talk to the Lost Blacksmith.");
            return;
        }
        if (environment.Strike(p))
            {
                visualEffects.Actions.PlayProjectile(origin, path, playerState.ClassIndex == 1, playerState.AdvancedClass);
                turns.EndTurn();
                return;
            }
            var e = dungeonState.At(p);
            if (e != null)
            {
                visualEffects.Actions.PlayProjectile(origin, path, playerState.ClassIndex == 1, playerState.AdvancedClass);
                if (combatService.ResolveHeroAttack(e, true)) environment.ElementalHit(e);
                turns.EndTurn();
                return;
            }
        }

        visualEffects.Actions.PlayProjectile(origin, path, playerState.ClassIndex == 1, playerState.AdvancedClass);
        expeditionJournal.Say("O disparo se perde na escuridao.", "The shot fades into the darkness.");
        turns.EndTurn();
    }

    internal void Skill()
    {
        int cost = heroCombatStats.AbilityCost;
        if (playerState.Energy < cost)
        {
            expeditionJournal.Say($"Habilidade requer {cost} de energia.", $"Your ability requires {cost} energy.");
            return;
        }

        var origin = playerState.Position;
        var targets = dungeonState.Enemies.Where(e => e.Health > 0 && dungeonState.Visible[e.Position.X, e.Position.Y] && dungeonState.Los(origin,e.Position) && GameRules.Dist(origin, e.Position) <= heroCombatStats.AbilityRange).OrderBy(e => GameRules.Dist(origin, e.Position)).ToList();
        if (targets.Count == 0 && environment.ElementalMage)
        {
            playerState.Energy -= cost;
            environment.ElementalArea(origin, heroCombatStats.AbilityRange);
            visualEffects.Actions.PlaySkill(1, origin, Array.Empty<Vector2I>(), heroCombatStats.AbilityRange, playerState.AdvancedClass);
            expeditionJournal.Say(AdvancementText.Primary(playerState.AdvancedClass, false) + "!", AdvancementText.Primary(playerState.AdvancedClass, true) + "!");
            turns.EndTurn();
            return;
        }
        if (targets.Count == 0)
        {
            expeditionJournal.Say("Nenhum alvo ao alcance da habilidade.", "No target within ability range.");
            return;
        }
        var chosenTarget = visualEffects.Focus != null && targets.Contains(visualEffects.Focus) ? visualEffects.Focus : targets[0];
        var targetPosition = chosenTarget.Position;
        if (playerState.ClassIndex != 3)
            visualEffects.Actions.PlaySkill(playerState.ClassIndex, origin, playerState.ClassIndex < 2 ? targets.Select(enemy => enemy.Position) : new[] { chosenTarget.Position }, heroCombatStats.AbilityRange, playerState.AdvancedClass);
        environment.ElementalArea(origin, heroCombatStats.AbilityRange);
        playerState.Energy -= cost;
        expeditionJournal.Say(playerState.AdvancedClass == AdvancedClass.None ? UiTheme.Skills[playerState.ClassIndex] + "!" : AdvancementText.Primary(playerState.AdvancedClass,false) + "!", playerState.AdvancedClass == AdvancedClass.None ? UiTheme.EnglishSkills[playerState.ClassIndex] + "!" : AdvancementText.Primary(playerState.AdvancedClass,true) + "!");
        if (playerState.ClassIndex < 2)
        {
            foreach (Enemy e in targets)
            {
                if (playerState.Health <= 0 || playerState.Position != origin) break;
                if (combatService.ResolveHeroAttack(e, false, true)) environment.ElementalHit(e);
            }
        }
        else
        {
            bool hit = combatService.ResolveHeroAttack(chosenTarget, false, true);
            if (playerState.ClassIndex == 3)
            {
                var destination = targetPosition + (targetPosition - origin);
                bool free = dungeonState.Walk(destination) && dungeonState.At(destination) == null
                    && !(dungeonState.IsMerchantFloor && destination == dungeonState.MerchantPosition)
                    && !(dungeonState.BlacksmithRoom.HasValue && destination == dungeonState.BlacksmithPosition)
                    && !(dungeonState.Environment.Fixtures.TryGetValue(destination, out var fixture) && fixture is Fixture.OilBarrel or Fixture.WallTorch);
                if (hit && playerState.Health > 0 && playerState.Position == origin && free)
                {
                    playerState.Position = destination;
                    Pickup();
                    if (playerState.AdvancedClass == AdvancedClass.Shadowblade) { playerState.GuardTurns = 2; playerState.GuardBonus = 4; }
                    expeditionJournal.Say("Voce surge atras do inimigo.", "You emerge behind the enemy.");
                }
                visualEffects.Actions.PlayRogueStep(origin,targetPosition,playerState.Position == destination ? destination : targetPosition);
            }
        }
        turns.EndTurn(playerState.ClassIndex == 3, previousPlayer: origin);
    }

    internal void Drink()
    {
        if (inventoryState.Potions == 0)
        {
            expeditionJournal.Say("Voce nao tem pocoes.", "You have no potions.");
            return;
        }

        if (playerState.Health == playerState.MaxHealth)
        {
            expeditionJournal.Say("Sua vida ja esta cheia.", "Your health is already full.");
            return;
        }

        var healing = RollPotion(random.Generator);
        expeditionJournal.LastPotionRoll = healing.Total;
        expeditionJournal.LastPotionHealing = Math.Min(playerState.MaxHealth - playerState.Health, healing.Total);
        inventoryState.Potions--;
        vitals.Heal(expeditionJournal.LastPotionHealing);
        visualEffects.Sounds.Play("potion");
        expeditionJournal.Say($"Pocao: 2d10 [{healing.First}+{healing.Second}] = {healing.Total}. Curou {expeditionJournal.LastPotionHealing} PV.", $"Potion: 2d10 [{healing.First}+{healing.Second}] = {healing.Total}. Healed {expeditionJournal.LastPotionHealing} HP.");
        turns.EndTurn();
    }
}
