namespace Abyss.Ports;
internal interface IEnemyBehavior
{
    bool Supports(Enemy enemy);
    void Act(Enemy enemy, bool evade, bool mayAttack);
}
