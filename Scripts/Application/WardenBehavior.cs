namespace Abyss.Application;
internal sealed class WardenBehavior(CombatService combatService, DungeonState dungeonState, ExpeditionJournal expeditionJournal, PlayerState playerState, EnemyNavigator navigator, WardenAbilities abilities) : IEnemyBehavior
{
    public bool Supports(Enemy enemy) => enemy.Glyph == 'B';
    public void Act(Enemy enemy, bool evade, bool mayAttack)
    {
        bool inside = dungeonState.StairsRoom.HasPoint(playerState.Position);
        if (!enemy.Alerted)
        {
            if (!inside)
                return;
            enemy.Alerted = true;
            expeditionJournal.Say("Voce entrou na sala da escada. O Guardiao desperta!", "You entered the stair room. The Warden awakens!");
        }
        if (abilities.Act(enemy, evade)) return;
        if (GameRules.Dist(playerState.Position, enemy.Position) == 1)
        {
            if (mayAttack)
                combatService.ResolveEnemyAttack(enemy, evade);
        }
        else
            for (int step = 0; step < 2 && GameRules.Dist(playerState.Position, enemy.Position) > 1; step++)
                if (!navigator.StepEnemy(enemy, playerState.Position)) break;
        return;
    }
}
