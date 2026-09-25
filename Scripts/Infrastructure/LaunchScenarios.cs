using Godot;
using System;
using System.Linq;

namespace Abyss.Infrastructure;
// Deterministic scenarios used by capture commands; normal runs leave the session on the home screen.
internal sealed class LaunchScenarios(GameSession game)
{
    internal void Configure(string[] args)
    {
        if (args.Contains("--english"))
            game.MenuState.English = true;
        if (args.Contains("--portuguese"))
            game.MenuState.English = false;
        if (args.Contains("--demo"))
        {
            game.PlayerState.ClassIndex = 1;
            game.Start(42073);
        }

        var view = args.FirstOrDefault(a => a.StartsWith("--view="));
        if (view != null)
        {
            var name = view.Substring(7);
            if (name == "help")
            {
                game.RunState.Screen = "pause";
                game.MenuState.PauseTab = 3;
            }
            else if (name == "language")
                game.MenuController.OpenLanguage("home");
            else if (name == "inventory")
            {
                game.RunState.Screen = "pause";
                game.MenuState.PauseTab = 1;
            }
            else if (name == "journal" || name == "settings")
            {
                game.RunState.Screen = "pause";
                game.MenuState.PauseTab = name == "journal" ? 2 : 4;
            }
            else
                game.RunState.Screen = name;
        }

        if (args.Contains("--damage-demo"))
        {
            game.PlayerState.ClassIndex = 0;
            game.Start(42073);
            game.DungeonState.Enemies.Clear();
            var pos = GameRules.Directions.Select(d => game.PlayerState.Position + d).First(game.DungeonState.Walk);
            var foe = new Enemy(pos, 'g', 1)
            {
                Health = 35,
                MaxHealth = 35
            };
            game.DungeonState.Enemies.Add(foe);
            game.DungeonGenerator.Reveal();
            game.CombatService.ResolveHeroAttack(foe);
            game.EndTurn();
        }

        if (args.Contains("--ranged-demo"))
        {
            game.PlayerState.ClassIndex = 1;
            game.Start(42073);
            game.DungeonState.Enemies.Clear();
            var pos = GameRules.Directions.Select(d => game.PlayerState.Position + d).First(game.DungeonState.Walk);
            var foe = new Enemy(pos, 's', 2)
            {
                Health = 35,
                MaxHealth = 35
            };
            game.DungeonState.Enemies.Add(foe);
            game.DungeonGenerator.Reveal();
            game.PlayerState.Health = game.PlayerState.MaxHealth - 10;
            game.InventoryState.Potions = 1;
            game.PlayerActions.Drink();
        }

        if (args.Contains("--inventory-demo"))
        {
            game.PlayerState.ClassIndex = 0;
            game.Start(42073);
            game.PlayerState.Level = 10;
            game.InventoryState.Potions = 2;
            game.InventoryState.EnergyPotions = 1;
            game.InventoryState.Backpack.Add(new Gear(GearKind.Sword, Rarity.Rare));
            game.InventoryState.Backpack.Add(new Gear(GearKind.Plate, Rarity.Epic));
            game.InventoryState.Backpack.Add(new Gear(GearKind.Sword, Rarity.Legendary));
            game.InventoryState.Backpack.Add(new Gear(GearKind.Amulet, Rarity.Epic));
            game.RunState.Screen = "pause";
            game.MenuState.PauseTab = 1;
            game.MenuState.InventoryIndex = 8;
        }

        if (args.Contains("--merchant-demo"))
        {
            game.PlayerState.ClassIndex = 0;
            game.Start(123);
            game.DungeonState.Floor = 16;
            game.PlayerState.Gold = 200;
            game.DungeonGenerator.Generate(true);
            game.PlayerState.Position = game.DungeonState.MerchantPosition + Vector2I.Down;
            game.PlayerActions.Interact();
            if (args.Contains("--trade-demo"))
                game.MenuController.HandleShop(Key.Enter);
            if (args.Contains("--room-demo"))
                game.RunState.Screen = "game";
        }
        ActionAnimationPreview.Configure(game, args);
    }
}
