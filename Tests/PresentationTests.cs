using Godot;
using System;
using System.Linq;

namespace Abyss.Tests;
internal sealed partial class RegressionSuite
{
    internal void TestPresentation()
    {
        game.MenuState.IsTesting = true;
        game.MenuState.English = false;
        game.PlayerState.ClassIndex = 0;
        game.RunState.Screen = "home";
        game.MenuState.MenuIndex = 0;
        game.GameInput.HandleKey(Key.Down);
        game.GameInput.HandleKey(Key.Enter);
        if (game.RunState.Screen != "language")
            throw new Exception("Home language navigation failed");
        game.GameInput.HandleKey(Key.Key2);
        game.GameInput.HandleKey(Key.Enter);
        if (!game.MenuState.English || game.RunState.Screen != "home" || game.Localization.ClassName(0) != "WARRIOR")
            throw new Exception("English selection failed");
        game.GameInput.HandleKey(Key.Enter);
        if (game.RunState.Screen != "classes")
            throw new Exception("Class screen failed");
        game.GameInput.HandleKey(Key.Key4);
        game.GameInput.HandleKey(Key.Enter);
        if (game.RunState.Screen != "game" || game.PlayerState.ClassIndex != 3 || !game.ExpeditionJournal.Entries[0].En.StartsWith("You entered"))
            throw new Exception("Localized start failed");
        int before = game.RunState.Turn;
        game.GameInput.HandleKey(Key.Escape);
        game.GameInput.HandleKey(Key.Key5);
        game.GameInput.HandleKey(Key.Enter);
        game.GameInput.HandleKey(Key.Key1);
        game.GameInput.HandleKey(Key.Enter);
        game.GameInput.HandleKey(Key.Escape);
        if (game.MenuState.English || game.RunState.Screen != "game" || game.RunState.Turn != before)
            throw new Exception("Pause language switching changed game state");
        game.RunState.Screen = "language";
        game.MenuState.LanguageReturn = "home";
        game.GameInput.HandleKey(Key.Key2);
        game.GameInput.HandleKey(Key.Escape);
        if (game.MenuState.English)
            throw new Exception("Cancelled language selection was applied");
        game.RunState.Screen = "game";
        game.GameInput.HandleKey(Key.Escape);
        var position = game.PlayerState.Position;
        int savedHp = game.PlayerState.Health, savedEnergy = game.PlayerState.Energy, savedPotions = game.InventoryState.Potions;
        game.GameInput.HandleKey(Key.Q);
        game.GameInput.HandleKey(Key.P);
        game.GameInput.HandleKey(Key.Space);
        game.GameInput.HandleKey(Key.W);
        if (game.RunState.Turn != before || game.PlayerState.Position != position || game.PlayerState.Health != savedHp || game.PlayerState.Energy != savedEnergy || game.InventoryState.Potions != savedPotions)
            throw new Exception("Paused actions changed the expedition");
        for (int i = 0; i < 45; i++)
            game.ExpeditionJournal.Say($"Registro {i}: " + new string ('x', 100), $"Entry {i}: " + new string ('x', 100));
        game.GameInput.HandleKey(Key.Key3);
        game.GameInput.HandleKey(Key.Down);
        if (game.MenuState.PauseTab != 2 || game.MenuState.JournalPage != 1 || game.ExpeditionJournal.Entries.Count < 45 || game.JournalFormatter.JournalLines().Any(line => line.Length > 91))
            throw new Exception("Journal history or pagination failed");
        game.GameInput.HandleKey(Key.End);
        game.GameInput.HandleKey(Key.Down);
        if (game.MenuState.JournalPage != (game.JournalFormatter.JournalLines().Count - 1) / UiTheme.JournalPageSize)
            throw new Exception("Journal last-page boundary failed");
        game.GameInput.HandleKey(Key.Home);
        game.GameInput.HandleKey(Key.Up);
        if (game.MenuState.JournalPage != 0)
            throw new Exception("Journal first-page boundary failed");
        game.GameInput.HandleKey(Key.Key5);
        game.GameInput.HandleKey(Key.Enter);
        game.GameInput.HandleKey(Key.Escape);
        if (game.RunState.Screen != "pause" || game.MenuState.PauseTab != 4)
            throw new Exception("Settings return tab lost");
        game.GameInput.HandleKey(Key.Down);
        game.GameInput.HandleKey(Key.Enter);
        if (game.RunState.Screen != "confirm_exit")
            throw new Exception("Missing exit confirmation");
        game.GameInput.HandleKey(Key.Down);
        game.GameInput.HandleKey(Key.Enter);
        if (game.RunState.Screen != "home" || game.RunState.Turn != before)
            throw new Exception("Pause main-menu action failed");
        game.RunState.Screen = "game";
        game.PlayerState.ClassIndex = 0;
        game.Start(17);
        game.DungeonState.Enemies.Clear();
        var pos = GameRules.Directions.Select(d => game.PlayerState.Position + d).First(game.DungeonState.Walk);
        var foe = new Enemy(pos, 'g', 1)
        {
            Health = 100,
            MaxHealth = 100
        };
        game.DungeonState.Enemies.Add(foe);
        game.DungeonGenerator.Reveal();
        game.RandomGenerator = new RegressionSuite.FixedRandom(20);
        game.CombatService.Hit(foe, 7);
        game.EndTurn();
        if (game.VisualEffects.HeroHurtRemaining <= 0 || foe.Hurt <= 0 || game.VisualEffects.Effects.Count != 2 || game.VisualEffects.FocusEnemy() != foe)
            throw new Exception("Damage effects missing");
        var glyph = UiTheme.ImpactGlyph(foe.Hurt);
        game.VisualEffects.AdvanceEffects(.2);
        if (UiTheme.ImpactGlyph(foe.Hurt) == glyph)
            throw new Exception("Damage characters did not animate");
        before = game.RunState.Turn;
        game.VisualEffects.AdvanceEffects(2);
        if (game.VisualEffects.Effects.Count != 0 || game.VisualEffects.HeroHurtRemaining != 0 || foe.Hurt != 0 || game.RunState.Turn != before)
            throw new Exception("Effects changed turns or failed to expire");
        game.CombatService.Hit(foe, 1000);
        if (game.VisualEffects.FocusEnemy() != foe)
            throw new Exception("Killing blow portrait vanished too early");
        game.VisualEffects.AdvanceEffects(2);
        if (game.VisualEffects.FocusEnemy() != null)
            throw new Exception("Stale dead target");
        var arts = AsciiArt.Heroes.Concat(new[] { AsciiArt.Merchant, AsciiArt.Rat, AsciiArt.Skeleton, AsciiArt.Goblin, AsciiArt.Warden, AsciiArt.Unknown, AsciiArt.Tower, AsciiArt.Camp, AsciiArt.Globe, AsciiArt.Book, AsciiArt.Grave, AsciiArt.Crown });
        if (arts.Any(a => a.Any(c => c != '\n' && (c < 32 || c > 126))))
            throw new Exception("Non-ASCII art");
        if (AsciiArt.Heroes.Distinct().Count() != 4)
            throw new Exception("Portraits not unique");
        var testPath = "user://language-test-" + Guid.NewGuid().ToString("N") + ".cfg";
        try
        {
            game.MenuState.English = true;
            game.LanguagePreferences.SaveLanguage(testPath);
            game.MenuState.English = false;
            game.LanguagePreferences.LoadLanguage(testPath);
            if (!game.MenuState.English)
                throw new Exception("English preference not persisted");
            game.MenuState.English = false;
            game.LanguagePreferences.SaveLanguage(testPath);
            game.MenuState.English = true;
            game.LanguagePreferences.LoadLanguage(testPath);
            if (game.MenuState.English)
                throw new Exception("Portuguese preference not persisted");
        }
        finally
        {
            DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(testPath));
        }

        game.RunState.Screen = "game";
        int helpTurn = game.RunState.Turn;
        game.GameInput.HandleKey(Key.H);
        if (game.RunState.Screen != "game" || game.RunState.Turn != helpTurn)
            throw new Exception("Removed H shortcut still acts");
        game.GameInput.HandleKey(Key.Escape);
        game.GameInput.HandleKey(Key.Key4);
        if (game.MenuState.PauseTab != 3)
            throw new Exception("Help tab order failed");
        for (int topic = 0; topic < 7; topic++)
        {
            game.MenuState.HelpTopic = topic;
            foreach (bool language in new[]
            {
                false,
                true
            }

            )
            {
                game.MenuState.English = language;
                if (game.Localization.HelpText(topic).Split('\n').Any(line => line.Length > 89))
                    throw new Exception("Help topic exceeds panel width");
            }
        }

        game.MenuState.HelpTopic = 0;
        game.GameInput.HandleKey(Key.Down);
        if (game.MenuState.HelpTopic != 1 || game.RunState.Turn != helpTurn)
            throw new Exception("Help navigation changed game state");
        game.GameInput.HandleKey(Key.Key5);
        if (game.MenuState.PauseTab != 4)
            throw new Exception("Settings tab order failed");
        for (int mode = 1; mode <= 3; mode++)
        {
            if (UiTheme.AsciiGlow(mode, .5f, .5f, 0) == UiTheme.AsciiGlow(mode, .5f, .5f, 1))
                throw new Exception("ASCII light does not animate");
            for (int t = 0; t < 100; t++)
                if (UiTheme.AsciiGlow(mode, .5f, .5f, t / 10.0) < .7 || UiTheme.AsciiGlow(mode, .5f, .5f, t / 10.0) > 1.3)
                    throw new Exception("ASCII light exceeds subtle brightness bounds");
        }

        game.MenuState.IsTesting = false;
    }
}
