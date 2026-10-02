using System;
using System.Collections.Generic;

namespace Abyss.Application;
internal sealed class EnemyAi(IReadOnlyList<IEnemyBehavior> behaviors)
{
    internal void ActEnemy(Enemy enemy, bool evade = false, bool mayAttack = true)
    {
        if (enemy.BlindTurns > 0 || enemy.FrozenTurns > 0)
        {
            enemy.BlindTurns = Math.Max(0, enemy.BlindTurns - 1);
            enemy.FrozenTurns = Math.Max(0, enemy.FrozenTurns - 1);
            return;
        }
        foreach (var behavior in behaviors)
            if (behavior.Supports(enemy))
            {
                behavior.Act(enemy, evade, mayAttack);
                return;
            }

        throw new InvalidOperationException($"No behavior registered for {enemy.Glyph}.");
    }
}
