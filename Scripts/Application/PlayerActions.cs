using Godot;
using System;
using System.Linq;
using static Abyss.Rules.TabletopRules;

namespace Abyss.Application;
internal sealed class PlayerActions
{
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
    internal PlayerActions(CombatService combatService, IRunLifecycle lifecycle, DungeonState dungeonState, ITurnScheduler turns, ExpeditionJournal expeditionJournal, HeroCombatStats heroCombatStats, InventoryState inventoryState, LootService lootService, MenuState menuState, PlayerState playerState, RunState runState, VisualEffects visualEffects, RandomStream random)
    {
        this.combatService = combatService;
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
            playerState.Position = p;
            Pickup();
        }

        turns.EndTurn(previousPlayer: previousPlayer);
    }

    internal void Pickup()
    {
        if (!dungeonState.Items.Remove(playerState.Position, out char g))
            return;
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
        var p = playerState.Position;
        for (int i = 0; i < range; i++)
        {
            p += d;
            if (!dungeonState.Walk(p))
                break;
            var e = dungeonState.At(p);
            if (e != null)
            {
                combatService.ResolveHeroAttack(e, true);
                turns.EndTurn();
                return;
            }
        }

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

        var targets = dungeonState.Enemies.Where(e => dungeonState.Visible[e.Position.X, e.Position.Y] && GameRules.Dist(playerState.Position, e.Position) <= heroCombatStats.AbilityRange).OrderBy(e => GameRules.Dist(playerState.Position, e.Position)).ToList();
        if (targets.Count == 0)
        {
            expeditionJournal.Say("Nenhum alvo ao alcance da habilidade.", "No target within ability range.");
            return;
        }

        playerState.Energy -= cost;
        expeditionJournal.Say(UiTheme.Skills[playerState.ClassIndex] + "!", UiTheme.EnglishSkills[playerState.ClassIndex] + "!");
        if (playerState.ClassIndex < 2)
            foreach (Enemy e in targets)
                combatService.ResolveHeroAttack(e, false, true);
        else
            combatService.ResolveHeroAttack(visualEffects.Focus != null && targets.Contains(visualEffects.Focus) ? visualEffects.Focus : targets[0], false, true);
        turns.EndTurn(playerState.ClassIndex == 3);
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
        playerState.Health += expeditionJournal.LastPotionHealing;
        expeditionJournal.Say($"Pocao: 2d10 [{healing.First}+{healing.Second}] = {healing.Total}. Curou {expeditionJournal.LastPotionHealing} PV.", $"Potion: 2d10 [{healing.First}+{healing.Second}] = {healing.Total}. Healed {expeditionJournal.LastPotionHealing} HP.");
        turns.EndTurn();
    }
}
