namespace Abyss.Application;
internal sealed class WardenBehavior(CombatService combatService, DungeonState dungeonState, ExpeditionJournal expeditionJournal, PlayerState playerState, EnemyNavigator navigator) : IEnemyBehavior
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
        if (inside && GameRules.Dist(playerState.Position, enemy.Position) == 1)
        {
            if (mayAttack)
                combatService.ResolveEnemyAttack(enemy, evade);
        }
        else
            navigator.StepEnemy(enemy, inside ? playerState.Position : dungeonState.Stairs, true);
        return;
    }
}
