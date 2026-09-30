using System;
using static Abyss.Rules.TabletopRules;

namespace Abyss.Application;
internal sealed class CombatService
{
    private readonly DungeonState dungeonState;
    private readonly ExpeditionJournal expeditionJournal;
    private readonly HeroCombatStats heroCombatStats;
    private readonly InventoryState inventoryState;
    private readonly Localization localization;
    private readonly LootService lootService;
    private readonly PlayerState playerState;
    private readonly ProgressionService progressionService;
    private readonly RunState runState;
    private readonly VisualEffects visualEffects;
    private readonly RandomStream random;
    internal CombatService(DungeonState dungeonState, ExpeditionJournal expeditionJournal, HeroCombatStats heroCombatStats, InventoryState inventoryState, Localization localization, LootService lootService, PlayerState playerState, ProgressionService progressionService, RunState runState, VisualEffects visualEffects, RandomStream random)
    {
        this.dungeonState = dungeonState;
        this.expeditionJournal = expeditionJournal;
        this.heroCombatStats = heroCombatStats;
        this.inventoryState = inventoryState;
        this.localization = localization;
        this.lootService = lootService;
        this.playerState = playerState;
        this.progressionService = progressionService;
        this.runState = runState;
        this.visualEffects = visualEffects;
        this.random = random;
    }

    internal int AttackBonus(Enemy enemy, bool ranged, bool ability)
    {
        int bonus = (ranged || ability ? heroCombatStats.SpellBonus : heroCombatStats.MeleeBonus) + (ability ? 2 : 0);
        if (ranged && !ability && GameRules.Dist(playerState.Position, enemy.Position) > 5)
            bonus -= 2;
        return bonus;
    }

    internal int TargetDefense(Enemy enemy, bool ranged, bool ability) => enemy.Armor;
    internal int ChanceAgainst(Enemy enemy, bool ranged, bool ability) => HitChance(AttackBonus(enemy, ranged, ability), TargetDefense(enemy, ranged, ability), ability && playerState.ClassIndex == 3 ? 19 : 20);
    internal void ResolveHeroAttack(Enemy enemy, bool ranged = false, bool ability = false)
    {
        if (!ranged && !ability && !heroCombatStats.CanMelee)
            return;
        if (!ranged && !ability && (GameRules.Dist(playerState.Position, enemy.Position) != 1 || !dungeonState.Los(playerState.Position, enemy.Position)))
            return;
        visualEffects.Focus = enemy;
        visualEffects.FocusHold = UiTheme.HurtDuration;
        var roll = ResolveAttack(random.Generator.Next(1, 21), AttackBonus(enemy, ranged, ability), TargetDefense(enemy, ranged, ability), ability && playerState.ClassIndex == 3 ? 19 : 20);
        string total = $"d20({roll.Natural}){UiTheme.Signed(roll.Bonus)}={roll.Total} vs {roll.Defense}";
        expeditionJournal.LastRollPt = $"Voce: {total}";
        expeditionJournal.LastRollEn = $"You: {total}";
        if (!roll.Hit)
        {
            expeditionJournal.Say($"Voce -> {FloorEventText.EnemyName(enemy, false)}: {total}. Errou.", $"You -> {FloorEventText.EnemyName(enemy, true)}: {total}. You miss.");
            return;
        }

        var dice = ability ? heroCombatStats.AbilityDice : ranged ? heroCombatStats.ShotDice : heroCombatStats.MeleeDice;
        int damage = RollDamage(random.Generator, dice, roll.Critical);
        if (inventoryState.Weapon is Gear weapon && weapon.Quality >= Rarity.Epic)
        {
            int extra = random.Generator.Next(1, weapon.Quality == Rarity.Legendary ? 7 : 5);
            damage += extra;
            expeditionJournal.Say($"Impacto do equipamento: +{extra} dano.", $"Equipment impact: +{extra} damage.");
            if (weapon.Quality == Rarity.Legendary)
                playerState.Health = Math.Min(playerState.MaxHealth, playerState.Health + Math.Min(2, Math.Max(0, enemy.Health)));
        }

        expeditionJournal.Say($"Voce -> {FloorEventText.EnemyName(enemy, false)}: {total}. {(roll.Critical ? "CRITICO! " : "")}{damage} dano ({dice}).", $"You -> {FloorEventText.EnemyName(enemy, true)}: {total}. {(roll.Critical ? "CRITICAL! " : "")}{damage} damage ({dice}).");
        Hit(enemy, damage, false);
    }

    internal void ResolveEnemyAttack(Enemy enemy, bool evade)
    {
        if (GameRules.Dist(playerState.Position, enemy.Position) != 1 || !dungeonState.Los(enemy.Position, playerState.Position))
            return;
        if (enemy.Glyph == 'B' && (!enemy.Alerted || !dungeonState.StairsRoom.HasPoint(playerState.Position)))
            return;
        var roll = ResolveAttack(random.Generator.Next(1, 21), enemy.AttackBonus, heroCombatStats.Defense + (evade ? 4 : 0));
        string total = $"d20({roll.Natural}){UiTheme.Signed(roll.Bonus)}={roll.Total} vs {roll.Defense}";
        expeditionJournal.LastRollPt = $"{FloorEventText.EnemyName(enemy, false)}: {total}";
        expeditionJournal.LastRollEn = $"{FloorEventText.EnemyName(enemy, true)}: {total}";
        if (!roll.Hit)
        {
            expeditionJournal.Say($"{FloorEventText.EnemyName(enemy, false)} -> voce: {total}. Errou.", $"{FloorEventText.EnemyName(enemy, true)} -> you: {total}. Missed.");
            return;
        }

        int damage = Math.Max(1, RollDamage(random.Generator, enemy.Dice, roll.Critical) - inventoryState.DamageReduction);
        playerState.Health = Math.Max(0, playerState.Health - damage);
        visualEffects.HeroHurt(enemy, damage);
        expeditionJournal.Say($"{FloorEventText.EnemyName(enemy, false)} -> voce: {total}. {(roll.Critical ? "CRITICO! " : "")}-{damage} PV.", $"{FloorEventText.EnemyName(enemy, true)} -> you: {total}. {(roll.Critical ? "CRITICAL! " : "")}-{damage} HP.");
        if (playerState.Health == 0)
            runState.Screen = "dead";
    }

    internal void Hit(Enemy e, int damage, bool report = true)
    {
        if (e.Health <= 0) return;
        e.Health -= damage;
        visualEffects.EnemyHurt(e, damage);
        if (report)
            expeditionJournal.Say($"Voce atinge {FloorEventText.EnemyName(e, false)} por {damage}.", $"You hit {FloorEventText.EnemyName(e, true)} for {damage} damage.");
        if (e.Health > 0)
            return;
        dungeonState.Enemies.Remove(e);
        playerState.Kills++;
        if (inventoryState.Equipped[2] is Gear charm && charm.Quality >= Rarity.Epic)
            playerState.Energy = Math.Min(playerState.MaxEnergy, playerState.Energy + (charm.Quality == Rarity.Legendary ? 2 : 1));
        int goldReward = lootService.RollEnemyGold(e.Glyph, e.Depth);
        playerState.Gold += goldReward;
        int reward = GameRules.EnemyXp(e.Glyph, e.Depth);
        if (goldReward > 0)
            expeditionJournal.Say($"{FloorEventText.EnemyName(e, false)} deixou {goldReward} ouro.", $"{FloorEventText.EnemyName(e, true)} dropped {goldReward} gold.");
        expeditionJournal.Say($"{FloorEventText.EnemyName(e, false)} derrotado. +{reward} XP.", $"{FloorEventText.EnemyName(e, true)} defeated. +{reward} XP.");
        if (e.IsElite && random.Generator.NextDouble() < .35)
        {
            var eliteLoot = lootService.DropEquipment(e.Depth, true);
            expeditionJournal.Say($"Elite: {localization.GearNameFor(eliteLoot, false)}.", $"Elite: {localization.GearNameFor(eliteLoot, true)}.");
        }
        if (e.Glyph == 'B')
        {
            inventoryState.Potions++;
            playerState.Energy = Math.Min(playerState.MaxEnergy, playerState.Energy + 3);
            var loot = lootService.DropEquipment(e.Depth, true);
            expeditionJournal.Say($"Guardiao: {localization.GearNameFor(loot, false)}.", $"Warden: {localization.GearNameFor(loot, true)}.");
            expeditionJournal.Say("Guardiao derrotado! +1 pocao, +3 energia. A descida esta livre.", "Warden defeated! +1 potion, +3 energy. The descent is open.");
        }

        progressionService.GainXp(reward);
    }
}
