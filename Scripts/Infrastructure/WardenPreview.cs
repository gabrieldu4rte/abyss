using Godot;
using System;
using System.Linq;
namespace Abyss.Infrastructure;
internal static class WardenPreview
{
    internal static void Configure(GameSession game, string[] args)
    {
        var option = args.FirstOrDefault(a => a.StartsWith("--warden-demo="));
        if (option == null) return;
        int biome = Math.Clamp(int.Parse(option.Split('=')[1]), 0, 3);
        game.Start(712);
        var world = game.DungeonState;
        world.Floor = biome * 5 + 5;
        world.Enemies.Clear(); world.Items.Clear(); world.Environment.Clear(); world.Modifier = FloorModifier.None;
        world.Environment.Biome = (Biome)biome;
        world.StairsRoom = new Rect2I(22, 7, 15, 13); world.Stairs = new Vector2I(24, 9);
        for (int y = 0; y < GameRules.Height; y++)
            for (int x = 0; x < GameRules.Width; x++) world.Tiles[x, y] = world.StairsRoom.HasPoint(new Vector2I(x, y)) ? '.' : '#';
        Array.Clear(world.Explored);
        game.PlayerState.Position = new Vector2I(30, 13);
        game.PlayerState.Health = game.PlayerState.MaxHealth = 100;
        var boss = new Enemy(new Vector2I(28, 13), 'B', world.Floor) { Alerted = true, AbilityCooldown = 0 };
        world.Enemies.Add(boss); game.DungeonGenerator.Reveal();
        var ability = new WardenAbilities(game.CombatService, world, game.PlayerState, game.ExpeditionJournal, game.VisualEffects);
        ability.Act(boss, false);
        if (args.Contains("--warden-cast"))
        {
            ability.Act(boss, false); ability.Act(boss, false);
            game.VisualEffects.AdvanceEffects(.35);
        }
    }
}
