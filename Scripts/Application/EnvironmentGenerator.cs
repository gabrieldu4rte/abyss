using Godot;
using System;
using System.Linq;

namespace Abyss.Application;
internal sealed class EnvironmentGenerator(DungeonState dungeon, PlayerState player, RunState run)
{
    internal void Generate()
    {
        var world = dungeon.Environment;
        int poison = world.HeroPoisonTurns;
        world.Clear();
        world.HeroPoisonTurns = poison;
        world.Biome = (Biome)(GameRules.CycleIndex(dungeon.Floor) % 4);
        var random = new Random(unchecked(run.Seed * 397 ^ dungeon.Floor * 7919));
        var free = new System.Collections.Generic.List<Vector2I>();
        for (int y = 1; y < GameRules.Height - 1; y++)
            for (int x = 1; x < GameRules.Width - 1; x++)
            {
                var p = new Vector2I(x, y);
                if (!dungeon.Walk(p) || p == dungeon.Stairs || p == player.Position || dungeon.Items.ContainsKey(p) || dungeon.At(p) != null || (dungeon.IsMerchantFloor && p == dungeon.MerchantPosition)) continue;
                free.Add(p);
                if (random.NextDouble() < .23)
                    world.Details[p] = world.Biome switch { Biome.Ruins => random.Next(2) == 0 ? ':' : '%', Biome.Cistern => ',', Biome.FungalCaves => random.Next(2) == 0 ? '"' : ';', _ => random.Next(2) == 0 ? ',' : ':' };
            }
        foreach (var center in free.Where(_ => random.NextDouble() < (world.Biome == Biome.Cistern ? .07 : .012)).ToArray())
            foreach (var p in free.Where(p => GameRules.Dist(p, center) <= 2))
                if (random.NextDouble() < .8) world.Details[p] = '~';
        var walls = free.Where(p => GameRules.Directions.Any(d => !dungeon.Walk(p + d))).OrderBy(_ => random.Next()).ToList();
        int torchCount = dungeon.IsMerchantFloor ? 3 : 5;
        foreach (var p in walls)
        {
            if (torchCount == 0) break;
            if (world.Fixtures.Keys.Any(q => GameRules.Dist(p, q) < 5)) continue;
            world.Fixtures[p] = Fixture.WallTorch;
            world.Details.Remove(p);
            torchCount--;
        }
        if (dungeon.IsMerchantFloor) return;
        var hazards = free.Where(p => GameRules.Dist(p, player.Position) > 5 && GameRules.Dist(p, dungeon.Stairs) > 2 && !world.Fixtures.ContainsKey(p) && (!world.Details.TryGetValue(p, out var detail) || detail != '~')).OrderBy(_ => random.Next()).ToList();
        int barrels = Math.Min(4, hazards.Count);
        foreach (var p in hazards.Take(barrels)) world.Fixtures[p] = Fixture.OilBarrel;
        foreach (var p in hazards.Skip(barrels).Take(3)) world.Fixtures[p] = Fixture.PoisonTrap;
    }
}
