using Godot;
using System;
using System.Linq;
namespace Abyss.Application;
internal sealed class NamedEquipmentEffects(InventoryState inventory, DungeonState dungeon, PlayerState player, CombatService combat, EnvironmentService environment, HeroCombatStats stats, RandomStream random, VisualEffects effects, ExpeditionJournal journal)
{
    internal void OnRangedHit(Enemy enemy)
    {
        if (inventory.Weapon?.Special == ItemId.ConductiveCrossbow && environment.Water(enemy.Position)) environment.Discharge(enemy.Position);
    }
    internal void OnHit(Enemy enemy, int natural, bool critical, bool physical)
    {
        var id = inventory.Weapon?.Special ?? ItemId.None;
        if (id == ItemId.SparkSword && physical && dungeon.Environment.Oil.Contains(enemy.Position) && random.Generator.NextDouble() < .5)
            environment.Ignite(enemy.Position);
        if (id == ItemId.VenomBow && natural == 20 && enemy.Health > 0 && enemy.HomeBiome != Biome.FungalCaves)
        {
            dungeon.Environment.PoisonedEnemies[enemy] = 3;
            journal.Say("Os esporos da flecha envenenam o alvo.", "The arrow's spores poison the target.");
        }
        if (id != ItemId.SpellforgeBlade || !critical) return;
        var origin = player.Position;
        var targets = dungeon.Enemies.Where(e => e.Health > 0 && GameRules.Dist(e.Position, origin) <= 2 && dungeon.Los(origin, e.Position)).ToArray();
        var cells = new System.Collections.Generic.List<Vector2I>();
        for (int y = -2; y <= 2; y++) for (int x = -2; x <= 2; x++)
            if (Math.Abs(x) + Math.Abs(y) <= 2) cells.Add(origin + new Vector2I(x,y));
        effects.Actions.PlayWarden(Biome.Ruins, origin, cells);
        journal.Say("A Forja das Magias libera um impacto sismico!", "The Spellforge releases a seismic impact!");
        foreach (var target in targets)
        {
            int damage = random.Generator.Next(1,5) + random.Generator.Next(1,5) + stats.EffectiveAttributes.Str;
            combat.Hit(target, Math.Max(1,damage));
            if (target.Health > 0) Push(target, origin);
        }
    }
    internal void Push(Enemy enemy, Vector2I origin) => EnemyDisplacement.Push(dungeon, player, enemy, origin);
}
