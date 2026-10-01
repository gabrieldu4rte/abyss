using System;
using System.Collections.Generic;

namespace Abyss.Application;
internal sealed class EnemyAi(IReadOnlyList<IEnemyBehavior> behaviors)
{
    internal void ActEnemy(Enemy enemy, bool evade = false, bool mayAttack = true)
    {
        if (enemy.BlindTurns > 0) { enemy.BlindTurns--; return; }
        foreach (var behavior in behaviors)
            if (behavior.Supports(enemy))
            {
                behavior.Act(enemy, evade, mayAttack);
                return;
            }

        throw new InvalidOperationException($"No behavior registered for {enemy.Glyph}.");
    }
}
