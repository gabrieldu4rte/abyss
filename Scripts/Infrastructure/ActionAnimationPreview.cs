using Godot;
using System;
using System.Linq;
using System.Globalization;

namespace Abyss.Infrastructure;

internal static class ActionAnimationPreview
{
    internal static void Configure(GameSession game, string[] args)
    {
        var option = args.FirstOrDefault(arg => arg.StartsWith("--action-demo="));
        if (option == null) return;
        string name = option.Split('=')[1];
        int hero = name switch { "warrior" => 0, "mage" or "bolt" => 1, "archer" or "arrow" => 2, "rogue" => 3, _ => throw new ArgumentException("Unknown action demo.") };
        game.PlayerState.ClassIndex = hero;
        game.Start(520);
        var world = game.DungeonState;
        world.Enemies.Clear();
        world.Items.Clear();
        for (int y = 0; y < GameRules.Height; y++)
            for (int x = 0; x < GameRules.Width; x++) world.Tiles[x, y] = x >= 20 && x <= 44 && y >= 5 && y <= 22 ? '.' : '#';
        Array.Clear(world.Explored);
        game.PlayerState.Position = new Vector2I(32, 13);
        bool shot = name is "bolt" or "arrow";
        var target = game.PlayerState.Position + Vector2I.Right * (shot ? (hero == 1 ? 6 : 8) : 2);
        world.Enemies.Add(new Enemy(target, 'g', 1) { Health = 100, MaxHealth = 100 });
        if (!shot && hero < 2)
        {
            world.Enemies.Add(new Enemy(game.PlayerState.Position + Vector2I.Left * 2, 's', 1) { Health = 100, MaxHealth = 100 });
            world.Enemies.Add(new Enemy(game.PlayerState.Position + Vector2I.Down * 2, 'r', 1) { Health = 100, MaxHealth = 100 });
        }
        game.DungeonGenerator.Reveal();
        if (shot) game.PlayerActions.Shoot(Vector2I.Right);
        else game.PlayerActions.Skill();
        var time = args.FirstOrDefault(arg => arg.StartsWith("--effect-time="));
        if (time != null && double.TryParse(time.Split('=')[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds))
            game.VisualEffects.AdvanceEffects(Math.Clamp(seconds, 0, 2));
    }
}
