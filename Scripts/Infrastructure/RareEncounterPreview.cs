using Godot;
using System;
using System.Linq;
namespace Abyss.Infrastructure;
internal static class RareEncounterPreview
{
    internal static void Configure(GameSession game, string[] args)
    {
        var option = args.FirstOrDefault(a => a.StartsWith("--rare-demo="));
        if (option == null) return;
        var modifier = Enum.Parse<FloorModifier>(option.Split('=')[1], true);
        game.Start(731); game.DungeonState.Floor = 7; game.DungeonGenerator.Generate(false);
        foreach (var enemy in game.DungeonState.Enemies) if (enemy.IsElite) { game.DungeonState.Enemies.Remove(enemy); break; }
        game.FloorEventGenerator.Generate(modifier, true);
        var elite = game.DungeonState.Enemies.Single(e => e.IsElite);
        game.PlayerState.Position = elite.Position + GameRules.Directions.First(d => game.DungeonState.Walk(elite.Position + d) && game.DungeonState.At(elite.Position + d) == null);
        game.VisualEffects.Focus = elite; game.DungeonGenerator.Reveal();
    }
}
