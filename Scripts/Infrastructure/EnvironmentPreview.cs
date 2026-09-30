using Godot;
using System;
using System.Linq;

namespace Abyss.Infrastructure;
internal static class EnvironmentPreview
{
    internal static void Configure(GameSession game, string[] args)
    {
        var option = args.FirstOrDefault(a => a.StartsWith("--biome-demo="));
        if (option == null) return;
        int biome = Math.Clamp(int.Parse(option.Split('=')[1]), 0, 3);
        game.Start(731);
        game.DungeonState.Floor = biome * 5 + 1;
        game.DungeonGenerator.Generate(false);
        if (args.Contains("--dark-demo"))
        {
            game.InventoryState.TorchEquipped = false;
            game.DungeonGenerator.Reveal();
        }
        else if (!args.Contains("--fog-demo"))
            for (int x = 0; x < GameRules.Width; x++)
                for (int y = 0; y < GameRules.Height; y++) game.DungeonState.Explored[x, y] = game.DungeonState.Visible[x, y] = true;
        if (args.Contains("--torch-inventory"))
        {
            game.RunState.Screen = "pause";
            game.MenuState.PauseTab = 1; game.MenuState.InventoryIndex = 5;
            game.InventoryState.SpareTorches = 3;
        }
        if (args.Contains("--fire-demo"))
        {
            var barrel = game.DungeonState.Environment.Fixtures.Where(f => f.Value == Fixture.OilBarrel).Select(f => (Vector2I?)f.Key).FirstOrDefault();
            if (barrel == null) return;
            game.EnvironmentService.Ignite(barrel.Value);
        }
    }
}
