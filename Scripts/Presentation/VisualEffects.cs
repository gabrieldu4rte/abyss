using System;
using System.Linq;
using System.Collections.Generic;

namespace Abyss.Presentation;
internal sealed class VisualEffects
{
    private readonly DungeonState dungeonState;
    private readonly PlayerState playerState;
    internal VisualEffects(DungeonState dungeonState, PlayerState playerState)
    {
        this.dungeonState = dungeonState;
        this.playerState = playerState;
    }

    internal double UiTime;
    internal double HeroHurtRemaining;
    internal double FocusHold;
    internal int HeroDamage;
    internal Enemy? Focus;
    internal readonly List<DamageEffect> Effects = new();
    internal void ResetEffects()
    {
        Effects.Clear();
        HeroHurtRemaining = FocusHold = 0;
        HeroDamage = 0;
        Focus = null;
    }

    internal void EnemyHurt(Enemy e, int damage)
    {
        e.Hurt = UiTheme.HurtDuration;
        e.LastDamage = damage;
        Focus = e;
        FocusHold = UiTheme.HurtDuration;
        Effects.Add(new DamageEffect { Position = e.Position, Damage = damage });
    }

    internal void HeroHurt(Enemy source, int damage)
    {
        HeroHurtRemaining = UiTheme.HurtDuration;
        HeroDamage = damage;
        if (FocusHold <= 0)
        {
            Focus = source;
            FocusHold = UiTheme.HurtDuration;
        }

        Effects.Add(new DamageEffect { Position = playerState.Position, Damage = damage, Hero = true });
    }

    internal void AdvanceEffects(double delta)
    {
        HeroHurtRemaining = Math.Max(0, HeroHurtRemaining - delta);
        FocusHold = Math.Max(0, FocusHold - delta);
        foreach (Enemy e in dungeonState.Enemies)
            e.Hurt = Math.Max(0, e.Hurt - delta);
        if (Focus != null && !dungeonState.Enemies.Contains(Focus))
            Focus.Hurt = Math.Max(0, Focus.Hurt - delta);
        foreach (DamageEffect fx in Effects)
            fx.Remaining -= delta;
        Effects.RemoveAll(fx => fx.Remaining <= 0);
    }

    internal Enemy? FocusEnemy()
    {
        if (Focus != null && ((Focus.Health <= 0 && FocusHold > 0) || (dungeonState.Enemies.Contains(Focus) && dungeonState.Visible[Focus.Position.X, Focus.Position.Y])))
            return Focus;
        Focus = dungeonState.Enemies.Where(e => dungeonState.Visible[e.Position.X, e.Position.Y]).OrderBy(e => GameRules.Dist(e.Position, playerState.Position)).FirstOrDefault();
        return Focus;
    }

    internal void CycleTarget()
    {
        var targets = dungeonState.Enemies.Where(e => dungeonState.Visible[e.Position.X, e.Position.Y]).OrderBy(e => GameRules.Dist(e.Position, playerState.Position)).ToList();
        if (targets.Count == 0)
        {
            Focus = null;
            return;
        }

        int i = Focus == null ? -1 : targets.IndexOf(Focus);
        Focus = targets[(i + 1) % targets.Count];
        FocusHold = 0;
    }
}
