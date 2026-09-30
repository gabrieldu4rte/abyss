using Godot;
using System;
using System.Linq;
namespace Abyss.Application;
internal sealed class WardenAbilities(CombatService combat, DungeonState dungeon, PlayerState player, ExpeditionJournal journal, VisualEffects effects)
{
    internal void Cancel(Enemy enemy)
    {
        enemy.AbilityCells.Clear(); enemy.AbilityWindup = 0; enemy.AbilityCooldown = 2;
    }
    internal bool Act(Enemy enemy, bool evade)
    {
        if (enemy.Health <= 0 || !enemy.IsWarden || !enemy.Alerted || !dungeon.StairsRoom.HasPoint(player.Position)) { Cancel(enemy); return false; }
        enemy.AbilityEnergy = Math.Min(6, enemy.AbilityEnergy + 1);
        if (enemy.AbilityWindup > 0)
        {
            if (--enemy.AbilityWindup > 0) return true;
            var cells = enemy.AbilityCells.ToArray();
            effects.Actions.PlayWarden(enemy.HomeBiome, enemy.Position, cells);
            journal.Say(EnemyText.Ability(enemy.HomeBiome, false) + "!", EnemyText.Ability(enemy.HomeBiome, true) + "!");
            bool hit = combat.ResolveWardenAbility(enemy, evade);
            if (enemy.HomeBiome == Biome.FungalCaves && hit) dungeon.Environment.HeroPoisonTurns = Math.Max(3, dungeon.Environment.HeroPoisonTurns);
            foreach (var cell in cells)
            {
                if (cell == dungeon.Stairs) continue;
                if (enemy.HomeBiome == Biome.Cistern)
                {
                    dungeon.Environment.Details[cell] = '~'; dungeon.Environment.Fire.Remove(cell); dungeon.Environment.Oil.Remove(cell);
                }
                if (enemy.HomeBiome == Biome.EmberForge && (!dungeon.Environment.Details.TryGetValue(cell, out var detail) || detail != '~'))
                    dungeon.Environment.Fire[cell] = dungeon.Modifier == FloorModifier.HotDraft ? 4 : 2;
            }
            if (enemy.HomeBiome == Biome.Cistern && hit) player.Energy = Math.Max(0, player.Energy - 2);
            enemy.AbilityCells.Clear(); enemy.AbilityCooldown = 4;
            return true;
        }
        if (enemy.AbilityCooldown > 0) { enemy.AbilityCooldown--; return false; }
        if (enemy.AbilityEnergy < 4 || GameRules.Dist(enemy.Position, player.Position) > 5 || !dungeon.Los(enemy.Position, player.Position)) return false;
        var direction = player.Position - enemy.Position;
        bool horizontal = Math.Abs(direction.X) >= Math.Abs(direction.Y);
        int sign = Math.Sign(horizontal ? direction.X : direction.Y);
        for (int y = dungeon.StairsRoom.Position.Y; y < dungeon.StairsRoom.End.Y; y++)
            for (int x = dungeon.StairsRoom.Position.X; x < dungeon.StairsRoom.End.X; x++)
            {
                var p = new Vector2I(x, y); var offset = p - enemy.Position;
                int forward = (horizontal ? offset.X : offset.Y) * sign;
                int lateral = Math.Abs(horizontal ? offset.Y : offset.X);
                bool marked = enemy.HomeBiome switch
                {
                    Biome.Ruins => GameRules.Dist(p, enemy.Position) <= 2,
                    Biome.Cistern => forward is >= 1 and <= 5 && lateral <= 1,
                    Biome.FungalCaves => GameRules.Dist(p, player.Position) <= 1,
                    _ => (offset.X == 0 || offset.Y == 0) && GameRules.Dist(p, enemy.Position) <= 4
                };
                if (marked && p != enemy.Position && dungeon.Walk(p) && dungeon.Los(enemy.Position, p)) enemy.AbilityCells.Add(p);
            }
        if (!enemy.AbilityCells.Contains(player.Position)) { enemy.AbilityCells.Clear(); return false; }
        effects.Sounds.Play("wardencharge");
        enemy.AbilityEnergy -= 4; enemy.AbilityWindup = 2;
        journal.Say($"{EnemyText.Name(enemy.Glyph, enemy.Depth, false)} prepara {EnemyText.Ability(enemy.HomeBiome, false)}. Evite as marcas [!] nos proximos dois turnos!", $"{EnemyText.Name(enemy.Glyph, enemy.Depth, true)} prepares {EnemyText.Ability(enemy.HomeBiome, true)}. Leave the [!] marks within two turns!");
        return true;
    }
}
