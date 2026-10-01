using Godot;
using System;
using System.Linq;

namespace Abyss.Infrastructure;
internal sealed class LaunchScenarios(GameSession game)
{
    internal void Configure(string[] args)
    {
        if (args.Contains("--english"))
            game.MenuState.English = true;
        if (args.Contains("--portuguese"))
            game.MenuState.English = false;
        if (args.Contains("--blacksmith-demo"))
        {
            game.Start(712);
            game.DungeonState.Floor = 4;
            game.DungeonGenerator.Generate(false, true);
            game.PlayerState.Position = game.DungeonState.BlacksmithPosition + Vector2I.Down;
            game.PlayerState.Gold = 500;
            game.InventoryState.Backpack.Add(NamedItemCatalog.Create(ItemId.SparkSword));
            game.DungeonGenerator.Reveal();
            game.PlayerActions.Interact();
            if (args.Contains("--upgrade-confirm")) game.BlacksmithService.Handle(Key.Enter);
        }
        var display = args.FirstOrDefault(a => a.StartsWith("--display-demo="));
        if (display != null)
        {
            string[] size = display.Split('=')[1].Split('x');
            int index = Array.IndexOf(SettingsController.Resolutions, new Vector2I(int.Parse(size[0]), int.Parse(size[1])));
            if (index >= 0) game.MenuState.ResolutionIndex = index;
            game.MenuState.Fullscreen = args.Contains("--fullscreen-preview");
            game.SettingsController.LoadDisplay();
        }
        var namedItem = args.FirstOrDefault(a => a.StartsWith("--item-demo="));
        if (namedItem != null)
        {
            game.Start(818);
            game.PlayerState.Level = 30;
            game.InventoryState.Backpack.Clear();
            Array.Clear(game.InventoryState.Equipped);
            var item = NamedItemCatalog.Create(Enum.Parse<ItemId>(namedItem.Split('=')[1]));
            game.InventoryState.Backpack.Add(item);
            game.InventoryState.Equipped[(int)item.Slot] = item;
            game.MenuState.InventoryIndex = 6;
            game.MenuState.PauseTab = 1;
            game.RunState.Screen = "pause";
        }
        if (args.Contains("--bestiary-demo"))
        {
            game.Start(712);
            game.MenuState.PauseTab = 3;
            game.MenuState.HelpTopic = 2;
            game.MenuState.BestiaryOpen = true;
            if (!args.Contains("--bestiary-locked"))
            {
                game.MenuState.BestiaryBiome = 2;
                game.MenuState.BestiaryEntry = 3;
                var enemy = new Enemy(game.PlayerState.Position, 'B', 15);
                game.BestiaryProgress.Record(enemy);
            }
            game.RunState.Screen = "pause";
        }
        if (args.Contains("--demo"))
        {
            game.PlayerState.ClassIndex = 1;
            game.Start(42073);
        }

        var view = args.FirstOrDefault(a => a.StartsWith("--view="));
        if (view != null)
        {
            var name = view.Substring(7);
            if (name == "main-settings")
                game.SettingsController.Open();
            else if (name == "help")
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
        var critical = args.FirstOrDefault(a => a.StartsWith("--critical-demo="));
        if (critical != null)
        {
            game.PlayerState.ClassIndex = Math.Clamp(int.Parse(critical.Split('=')[1]), 0, 3);
            game.Start(42073);
            game.PlayerState.Health = Math.Max(1, game.PlayerState.MaxHealth / 4);
            if (args.Contains("--view=pause")) game.RunState.Screen = "pause";
            if (args.Contains("--view=dead")) { game.PlayerState.Health = 0; game.RunState.Screen = "dead"; }
        }
    }
}
