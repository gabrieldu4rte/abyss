using Godot;
using System;
using System.Linq;
namespace Abyss.Application;
internal sealed class AdvancedAbilityService(PlayerState player, DungeonState dungeon, HeroCombatStats stats, CombatService combat, EnvironmentService environment, ExpeditionJournal journal, VisualEffects effects, ITurnScheduler turns)
{
    internal void Use()
    {
        var choice = player.AdvancedClass;
        if (choice == AdvancedClass.None || player.Health <= 0) return;
        int cost = stats.SecondaryCost;
        if (player.Energy < cost)
        {
            journal.Say($"Habilidade requer {cost} de energia.", $"Your ability requires {cost} energy."); return;
        }
        var origin = player.Position;
        var candidates = dungeon.Enemies.Where(e => e.Health > 0 && dungeon.Visible[e.Position.X,e.Position.Y] && dungeon.Los(origin,e.Position) && GameRules.Dist(origin,e.Position) <= stats.SecondaryRange).OrderBy(e => GameRules.Dist(origin,e.Position)).ToArray();
        if (choice != AdvancedClass.Sentinel && candidates.Length == 0)
        {
            journal.Say("Nenhum alvo ao alcance da habilidade.", "No target within ability range."); return;
        }
        var selected = effects.Focus != null && candidates.Contains(effects.Focus) ? effects.Focus : candidates.FirstOrDefault();
        var targets = choice switch
        {
            AdvancedClass.Sentinel => Array.Empty<Enemy>(),
            AdvancedClass.Pyromancer => candidates.Where(e => GameRules.Dist(e.Position,selected!.Position) <= 1 && dungeon.Los(selected.Position,e.Position)).ToArray(),
            AdvancedClass.Cryomancer or AdvancedClass.Ranger or AdvancedClass.Shadowblade => candidates,
            _ => new[] { selected! }
        };
        player.Energy -= cost;
        journal.Say(AdvancementText.Secondary(choice,false) + "!", AdvancementText.Secondary(choice,true) + "!");
        var cells = choice == AdvancedClass.Sentinel ? GameRules.Directions.Select(d => origin + d).Append(origin).ToArray() : targets.Select(e => e.Position).ToArray();
        effects.Actions.PlayAdvanced(choice,origin,cells,stats.SecondaryRange);
        if (choice == AdvancedClass.Sentinel) { player.GuardBonus = 6; player.GuardTurns = 3; }
        foreach (var enemy in targets)
        {
            if (player.Health <= 0 || player.Position != origin) break;
            if (choice == AdvancedClass.Shadowblade) { enemy.BlindTurns = Math.Max(enemy.BlindTurns,2); continue; }
            bool hit = combat.ResolveHeroAttack(enemy,false,true,stats.SecondaryDice,choice == AdvancedClass.Deadeye ? 4 : 0);
            if (hit) environment.ElementalHit(enemy);
            if (!hit || enemy.Health <= 0) continue;
            if (choice == AdvancedClass.Cryomancer) enemy.FrozenTurns = Math.Max(enemy.FrozenTurns,2);
            if (choice == AdvancedClass.Berserker) EnemyDisplacement.Push(dungeon,player,enemy,origin);
            if (choice == AdvancedClass.Assassin && enemy.HomeBiome != Biome.FungalCaves) dungeon.Environment.PoisonedEnemies[enemy] = 3;
        }
        if (choice == AdvancedClass.Pyromancer && player.Health > 0)
            foreach (var p in GameRules.Directions.Select(d => selected!.Position + d).Append(selected!.Position))
                if (dungeon.Walk(p) && dungeon.Los(selected.Position,p)) environment.Ignite(p,false);
        turns.EndTurn();
    }
}
