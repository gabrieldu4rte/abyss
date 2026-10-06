using System;
using static Abyss.Rules.TabletopRules;

namespace Abyss.Application;
internal sealed class CombatService
{
    internal event Action<Enemy, int, bool, bool>? HeroHit;
    internal event Action<Enemy>? HeroRangedHit;
    internal event Action<Enemy>? EnemyHit;
    private readonly HeroVitals vitals;
    private readonly BestiaryProgress bestiary;
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
    internal CombatService(DungeonState dungeonState, ExpeditionJournal expeditionJournal, HeroCombatStats heroCombatStats, InventoryState inventoryState, Localization localization, LootService lootService, PlayerState playerState, ProgressionService progressionService, RunState runState, VisualEffects visualEffects, RandomStream random, BestiaryProgress bestiary, HeroVitals vitals)
    {
        this.bestiary = bestiary;
        this.vitals = vitals;
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
        if (inventoryState.Weapon?.Special != ItemId.RevengeBow && ranged && !ability && GameRules.Dist(playerState.Position, enemy.Position) > 5)
            bonus -= 2;
        return bonus;
    }

    internal int TargetDefense(Enemy enemy, bool ranged, bool ability) => enemy.Armor;
    internal int ChanceAgainst(Enemy enemy, bool ranged, bool ability) => HitChance(AttackBonus(enemy, ranged, ability), TargetDefense(enemy, ranged, ability), ability && playerState.ClassIndex == 3 ? 19 : 20);
    internal bool ResolveHeroAttack(Enemy enemy, bool ranged = false, bool ability = false, DamageDice? overrideDice = null, int accuracyBonus = 0)
    {
        if (enemy.Health <= 0 || playerState.Health <= 0) return false;
        if (!ranged && !ability && !heroCombatStats.CanMelee)
            return false;
        if (!ranged && !ability && (GameRules.Dist(playerState.Position, enemy.Position) != 1 || !dungeonState.Los(playerState.Position, enemy.Position)))
            return false;
        if (!ranged && !ability) visualEffects.Sounds.Play("swing");
        visualEffects.Focus = enemy;
        visualEffects.FocusHold = UiTheme.HurtDuration;
        bool sacrifice = playerState.ClassIndex == 3 && inventoryState.Weapon?.Special == ItemId.ExecutionerBlade;
        if (sacrifice)
        {
            int rescue = vitals.RescueSerial;
            vitals.Damage(1);
            visualEffects.HeroHurt(enemy, 1);
            expeditionJournal.Say("A Lamina do Algoz cobra 1 PV.", "The Executioner's Blade claims 1 HP.");
            if (playerState.Health == 0 || vitals.RescueSerial != rescue) return false;
        }
        int natural = random.Generator.Next(1, 21);
        string advantageRoll = "";
        bool floodedMelee = inventoryState.Weapon?.Special == ItemId.ExplorerBlade && !ranged && GameRules.Dist(playerState.Position, enemy.Position) == 1 && dungeonState.Environment.Details.TryGetValue(enemy.Position, out var terrain) && terrain == '~' && !dungeonState.Environment.Ice.ContainsKey(enemy.Position);
        if (floodedMelee || inventoryState.Weapon?.Special == ItemId.RevengeBow && (enemy.IsElite || enemy.IsWarden))
        {
            int second = random.Generator.Next(1, 21);
            advantageRoll = $"[{natural},{second}] ";
            natural = Math.Max(natural, second);
        }
        var roll = ResolveAttack(natural, AttackBonus(enemy, ranged, ability) + accuracyBonus, TargetDefense(enemy, ranged, ability), ability && playerState.ClassIndex == 3 ? 19 : 20);
        string total = $"{advantageRoll}d20({roll.Natural}){UiTheme.Signed(roll.Bonus)}={roll.Total} vs {roll.Defense}";
        expeditionJournal.LastRollPt = $"Voce: {total}";
        expeditionJournal.LastRollEn = $"You: {total}";
        if (!roll.Hit)
        {
            visualEffects.Sounds.Play("miss");
            expeditionJournal.Say($"Voce -> {FloorEventText.EnemyName(enemy, false)}: {total}. Errou.", $"You -> {FloorEventText.EnemyName(enemy, true)}: {total}. You miss.");
            return false;
        }

        var dice = overrideDice ?? (ability ? heroCombatStats.AbilityDice : ranged ? heroCombatStats.ShotDice : heroCombatStats.MeleeDice);
        int damage = RollDamage(random.Generator, dice, roll.Critical);
        if (playerState.ClassIndex != 1 && EnemyTraits.Armored(enemy.Glyph)) damage = Math.Max(1, damage - 1);
        if (floodedMelee) damage += 2;
        if ((ranged || ability && playerState.ClassIndex == 2) && inventoryState.Weapon?.Special == ItemId.TwilightBow && dungeonState.Visible[enemy.Position.X,enemy.Position.Y] && dungeonState.Los(playerState.Position,enemy.Position) && (enemy.Position-playerState.Position).LengthSquared() > Math.Pow(dungeonState.Modifier == FloorModifier.Blackout ? 2 : inventoryState.HasLight ? 5 : 3, 2)) damage += 3;
        if (sacrifice) damage += random.Generator.Next(1,7);
        if (inventoryState.Weapon is Gear weapon && weapon.Quality >= Rarity.Epic && weapon.Special == ItemId.None)
        {
            int extra = random.Generator.Next(1, weapon.Quality == Rarity.Legendary ? 7 : 5);
            damage += extra;
            expeditionJournal.Say($"Impacto do equipamento: +{extra} dano.", $"Equipment impact: +{extra} damage.");
            if (weapon.Quality == Rarity.Legendary)
                vitals.Heal(Math.Min(2, Math.Max(0, enemy.Health)));
        }

        expeditionJournal.Say($"Voce -> {FloorEventText.EnemyName(enemy, false)}: {total}. {(roll.Critical ? "CRITICO! " : "")}{damage} dano ({dice}).", $"You -> {FloorEventText.EnemyName(enemy, true)}: {total}. {(roll.Critical ? "CRITICAL! " : "")}{damage} damage ({dice}).");
        Hit(enemy, damage, false);
        HeroHit?.Invoke(enemy, roll.Natural, roll.Critical, playerState.ClassIndex != 1);
        if (ranged || ability && playerState.ClassIndex == 2) HeroRangedHit?.Invoke(enemy);
        return true;
    }

    internal void ResolveEnemyAttack(Enemy enemy, bool evade)
    {
        if (dungeonState.IsSanctuary(playerState.Position) || GameRules.Dist(playerState.Position, enemy.Position) > EnemyTraits.AttackRange(enemy.Glyph) || !dungeonState.Los(enemy.Position, playerState.Position))
            return;
        if (enemy.IsWarden && !enemy.Alerted)
            return;
        if (GameRules.Dist(playerState.Position, enemy.Position) > 1)
            visualEffects.Actions.PlayEnemyProjectile(enemy, playerState.Position);
        var roll = ResolveAttack(random.Generator.Next(1, 21), enemy.AttackBonus, heroCombatStats.Defense + (evade ? 4 : 0));
        string total = $"d20({roll.Natural}){UiTheme.Signed(roll.Bonus)}={roll.Total} vs {roll.Defense}";
        expeditionJournal.LastRollPt = $"{FloorEventText.EnemyName(enemy, false)}: {total}";
        expeditionJournal.LastRollEn = $"{FloorEventText.EnemyName(enemy, true)}: {total}";
        if (!roll.Hit)
        {
            visualEffects.Sounds.Play("miss");
            expeditionJournal.Say($"{FloorEventText.EnemyName(enemy, false)} -> voce: {total}. Errou.", $"{FloorEventText.EnemyName(enemy, true)} -> you: {total}. Missed.");
            return;
        }

        if (enemy.Glyph == 'i' && inventoryState.Has(ItemId.EternalForgeRobe))
        {
            expeditionJournal.Say("O robe repele as brasas.", "The robe repels the embers.");
            return;
        }
        int damage = Math.Max(1, RollDamage(random.Generator, enemy.Dice, roll.Critical) - inventoryState.DamageReduction);
        int rescue = vitals.RescueSerial;
        vitals.Damage(damage);
        if (playerState.Health > 0 && vitals.RescueSerial == rescue) EnemyHit?.Invoke(enemy);
        visualEffects.HeroHurt(enemy, damage);
        expeditionJournal.Say($"{FloorEventText.EnemyName(enemy, false)} -> voce: {total}. {(roll.Critical ? "CRITICO! " : "")}-{damage} PV.", $"{FloorEventText.EnemyName(enemy, true)} -> you: {total}. {(roll.Critical ? "CRITICAL! " : "")}-{damage} HP.");
        if (playerState.Health == 0)
            runState.Screen = "dead";
    }

    internal bool ResolveWardenAbility(Enemy enemy, bool evade)
    {
        if (enemy.HomeBiome == Biome.EmberForge && inventoryState.Has(ItemId.EternalForgeRobe)) return false;
        if (enemy.Health <= 0 || !enemy.IsWarden || !enemy.Alerted || dungeonState.IsSanctuary(playerState.Position) || !enemy.AbilityCells.Contains(playerState.Position) || !dungeonState.Los(enemy.Position, playerState.Position)) return false;
        int modifier = enemy.HomeBiome == Biome.Ruins ? enemy.Stats.Str : enemy.Stats.Int;
        var roll = ResolveAttack(random.Generator.Next(1, 21), enemy.Training + modifier + 1, heroCombatStats.Defense + (evade ? 4 : 0));
        string total = $"d20({roll.Natural}){UiTheme.Signed(roll.Bonus)}={roll.Total} vs {roll.Defense}";
        expeditionJournal.LastRollPt = $"{EnemyText.Ability(enemy.HomeBiome, false)}: {total}";
        expeditionJournal.LastRollEn = $"{EnemyText.Ability(enemy.HomeBiome, true)}: {total}";
        if (!roll.Hit)
        {
            visualEffects.Sounds.Play("miss");
            expeditionJournal.Say(expeditionJournal.LastRollPt + ". Errou.", expeditionJournal.LastRollEn + ". Missed.");
            return false;
        }
        var dice = new DamageDice(enemy.HomeBiome == Biome.Ruins ? 2 : 1, enemy.HomeBiome is Biome.Ruins or Biome.FungalCaves ? 4 : 6, modifier + enemy.Tier);
        int damage = Math.Max(1, RollDamage(random.Generator, dice, roll.Critical) - inventoryState.DamageReduction);
        vitals.Damage(damage);
        visualEffects.HeroHurt(enemy, damage);
        expeditionJournal.Say($"{expeditionJournal.LastRollPt}. -{damage} PV ({dice}).", $"{expeditionJournal.LastRollEn}. -{damage} HP ({dice}).");
        if (playerState.Health == 0) runState.Screen = "dead";
        return true;
    }

    internal void Hit(Enemy e, int damage, bool report = true)
    {
        if (e.Health <= 0) return;
        if (e.IsWarden && damage > 0 && !e.Alerted)
        {
            e.Alerted = true;
            expeditionJournal.Say("O dano desperta o Guardiao!", "The damage awakens the Warden!");
        }
        e.Health -= damage;
        visualEffects.EnemyHurt(e, damage);
        if (report)
            expeditionJournal.Say($"Voce atinge {FloorEventText.EnemyName(e, false)} por {damage}.", $"You hit {FloorEventText.EnemyName(e, true)} for {damage} damage.");
        if (e.Health > 0)
            return;
        dungeonState.Enemies.Remove(e);
        playerState.Kills++;
        if (inventoryState.Weapon?.Special == ItemId.EmberStaff && dungeonState.Environment.Fire.ContainsKey(e.Position)) playerState.Energy = Math.Min(playerState.MaxEnergy, playerState.Energy + 2);
        if (!bestiary.Record(e))
            expeditionJournal.Say("Nao foi possivel salvar o bestiario. O registro permanece nesta sessao.", "Could not save the bestiary. The record remains in this session.");
        if (inventoryState.Equipped[2] is Gear charm && charm.Quality >= Rarity.Epic && charm.Special == ItemId.None)
            playerState.Energy = Math.Min(playerState.MaxEnergy, playerState.Energy + (charm.Quality == Rarity.Legendary ? 2 : 1));
        int goldReward = lootService.RollEnemyGold(e.Glyph, e.Depth);
        playerState.Gold += goldReward;
        int reward = GameRules.EnemyXp(e.Glyph, e.Depth);
        if (goldReward > 0)
            expeditionJournal.Say($"{FloorEventText.EnemyName(e, false)} deixou {goldReward} ouro.", $"{FloorEventText.EnemyName(e, true)} dropped {goldReward} gold.");
        expeditionJournal.Say($"{FloorEventText.EnemyName(e, false)} derrotado. +{reward} XP.", $"{FloorEventText.EnemyName(e, true)} defeated. +{reward} XP.");
        if (e.IsElite && (inventoryState.Has(ItemId.HunterGreedAmulet) || random.Generator.NextDouble() < .35))
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
