using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Abyss.Tests;
internal sealed partial class RegressionSuite
{
    internal void TestArchitecture()
    {
        var host = new TestHost();
        var settings = new TestSettings();
        var isolated = new GameSession(host, new TestCanvas(), settings);
        isolated.GameInput.HandleKey(Key.F11);
        if (host.FullscreenChanges != 1)
            throw new Exception("Fullscreen bypassed the host port.");
        isolated.GameInput.HandleKey(Key.Key3);
        isolated.GameInput.HandleKey(Key.Enter);
        if (host.QuitCalls != 0 || isolated.RunState.Screen != "confirm_quit" || isolated.MenuState.ExitYes)
            throw new Exception("Quit must ask for confirmation with No selected.");
        isolated.GameInput.HandleKey(Key.Enter);
        if (isolated.RunState.Screen != "home" || host.QuitCalls != 0)
            throw new Exception("Declining quit must return home.");
        isolated.GameInput.HandleKey(Key.Enter);
        isolated.GameInput.HandleKey(Key.Escape);
        if (isolated.RunState.Screen != "home" || host.QuitCalls != 0)
            throw new Exception("Escape must cancel quitting.");
        isolated.GameInput.HandleKey(Key.Enter);
        isolated.GameInput.HandleKey(Key.Down);
        isolated.GameInput.HandleKey(Key.Enter);
        if (host.QuitCalls != 1)
            throw new Exception("Quit bypassed the host port.");
        isolated.MenuState.English = true;
        isolated.LanguagePreferences.SaveLanguage("memory");
        isolated.MenuState.English = false;
        isolated.LanguagePreferences.LoadLanguage("memory");
        if (!isolated.MenuState.English)
            throw new Exception("Language preferences bypassed the settings port.");
        var first = new TestBehavior(false);
        var second = new TestBehavior(true);
        var last = new TestBehavior(true);
        new EnemyAi(new IEnemyBehavior[] { first, second, last }).ActEnemy(new Enemy(Vector2I.Zero, 'r', 1));
        if (first.Actions != 0 || second.Actions != 1 || last.Actions != 0)
            throw new Exception("AI policy substitution or first-match dispatch failed.");
        var assembly = typeof(GameSession).Assembly;
        foreach (var type in assembly.GetTypes().Where(t => t.Namespace == "Abyss.Domain"))
        {
            if (typeof(Node).IsAssignableFrom(type))
                throw new Exception("Domain model inherits from a scene node.");
            var dependencies = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Select(f => f.FieldType);
            if (dependencies.Any(t => t.Namespace is "Abyss.Application" or "Abyss.Presentation" or "Abyss.Infrastructure"))
                throw new Exception($"Domain model {type.Name} depends on an outer layer.");
        }

        foreach (var type in assembly.GetTypes().Where(t => t.Namespace == "Abyss.Application" && t != typeof(GameSession)))
            if (type.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SelectMany(c => c.GetParameters()).Any(p => p.ParameterType == typeof(GameSession)))
                throw new Exception($"{type.Name} receives the entire session instead of explicit dependencies.");
        GD.Print("ARCHITECTURE AUDIT: platform substitutions, AI policy dispatch, and dependency boundaries passed.");
    }

    private sealed class TestHost : IGameHost
    {
        internal int FullscreenChanges, QuitCalls;
        public void RequestRedraw()
        {
        }

        public void Hide()
        {
        }

        public void Quit(int exitCode = 0) => QuitCalls++;
        public void ToggleFullscreen() => FullscreenChanges++;
    }

    private sealed class TestCanvas : IAsciiCanvas
    {
        public void DrawString(Font font, Vector2 position, string text, HorizontalAlignment alignment, float width, int fontSize, Color color)
        {
        }

        public void DrawSetTransform(Vector2 position, float rotation, Vector2 scale)
        {
        }
    }

    private sealed class TestSettings : ILanguageSettings
    {
        private readonly Dictionary<string, bool> values = new();
        public bool? LoadEnglish(string path) => values.TryGetValue(path, out var english) ? english : null;
        public bool SaveEnglish(string path, bool english)
        {
            values[path] = english;
            return true;
        }
    }

    private sealed class TestBehavior(bool supports) : IEnemyBehavior
    {
        internal int Actions;
        public bool Supports(Enemy enemy) => supports;
        public void Act(Enemy enemy, bool evade, bool mayAttack) => Actions++;
    }
}
