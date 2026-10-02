using Godot;
using System.Collections.Generic;

namespace Abyss.Application;
internal sealed class RoamingBehavior(CombatService combatService, DungeonState dungeonState, PlayerState playerState, RandomStream random, EnemyNavigator navigator) : IEnemyBehavior
{
    public bool Supports(Enemy enemy) => enemy.Glyph != 'B';
    public void Act(Enemy enemy, bool evade, bool mayAttack)
    {
        if (enemy.AttackCooldown > 0) enemy.AttackCooldown--;
        bool seesPlayer = !dungeonState.IsSanctuary(playerState.Position) && GameRules.Dist(enemy.Position, playerState.Position) < 13 && dungeonState.Los(enemy.Position, playerState.Position);
        if (seesPlayer)
        {
            enemy.Alerted = true;
            enemy.LastSeen = playerState.Position;
            enemy.SearchTurns = 6;
            enemy.PatrolTarget = null;
            if (GameRules.Dist(playerState.Position, enemy.Position) <= EnemyTraits.AttackRange(enemy.Glyph))
            {
                if (mayAttack && enemy.AttackCooldown == 0)
                {
                    combatService.ResolveEnemyAttack(enemy, evade);
                    if (EnemyTraits.AttackRange(enemy.Glyph) > 1) enemy.AttackCooldown = 2;
                }
            }
            else
                navigator.StepEnemy(enemy, playerState.Position);
            return;
        }

        if (enemy.Alerted && enemy.LastSeen.HasValue && enemy.SearchTurns > 0 && enemy.Position != enemy.LastSeen.Value)
        {
            navigator.StepEnemy(enemy, enemy.LastSeen.Value);
            enemy.SearchTurns--;
            return;
        }

        enemy.Alerted = false;
        enemy.LastSeen = null;
        enemy.SearchTurns = 0;
        if (!enemy.PatrolTarget.HasValue || enemy.PatrolTarget.Value == enemy.Position)
        {
            var destinations = new List<Vector2I>();
            for (int y = 1; y < GameRules.Height - 1; y++)
                for (int x = 1; x < GameRules.Width - 1; x++)
                {
                    var p = new Vector2I(x, y);
                    if (dungeonState.Walk(p) && !dungeonState.IsSanctuary(p) && p != enemy.Position && p != playerState.Position && dungeonState.At(p) == null)
                        destinations.Add(p);
                }

            enemy.PatrolTarget = destinations.Count == 0 ? null : destinations[random.Generator.Next(destinations.Count)];
        }

        if (enemy.PatrolTarget.HasValue && !navigator.StepEnemy(enemy, enemy.PatrolTarget.Value))
            enemy.PatrolTarget = null;
    }
}
