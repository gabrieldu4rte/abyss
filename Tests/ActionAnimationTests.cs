using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Abyss.Tests;

internal sealed partial class RegressionSuite
{
    internal void TestActionAnimations()
    {
        void Prepare(int hero)
        {
            game.PlayerState.ClassIndex = hero;
            game.Start(520);
            game.DungeonState.Enemies.Clear();
            game.DungeonState.Items.Clear();
        game.DungeonState.Environment.Clear();
            game.PlayerState.Position = new Vector2I(30, 13);
            for (int y = 1; y < GameRules.Height - 1; y++)
                for (int x = 1; x < GameRules.Width - 1; x++) game.DungeonState.Tiles[x, y] = '.';
            game.DungeonGenerator.Reveal();
        }
        foreach (int hero in new[] { 1, 2 })
            foreach (var direction in GameRules.Directions)
            {
                Prepare(hero);
                var origin = game.PlayerState.Position;
                var target = origin + direction * 3;
                var enemy = new Enemy(target, 'g', 1) { Health = 100, MaxHealth = 100 };
                game.DungeonState.Enemies.Add(enemy);
                game.PlayerActions.BeginAim();
                if (game.VisualEffects.Actions.Active) throw new Exception("Aim began a projectile before direction selection.");
                game.PlayerActions.Shoot(direction);
                var animation = game.VisualEffects.Actions.Animations.Single();
                if (animation.Origin != origin || animation.Path[^1] != target || animation.Path.Count != 4)
                    throw new Exception("Projectile did not stop at the first enemy.");
                if (animation.Path.Where((p, i) => p != origin + direction * i).Any()) throw new Exception("Projectile direction changed.");
                var early = animation.Sample().ToArray();
                int turns = game.RunState.Turn, health = game.PlayerState.Health, energy = game.PlayerState.Energy;
                game.VisualEffects.AdvanceEffects(.12);
                if (animation.Sample().SequenceEqual(early)) throw new Exception("Projectile does not animate.");
                game.VisualEffects.AdvanceEffects(2);
                if (game.VisualEffects.Actions.Active || turns != game.RunState.Turn || health != game.PlayerState.Health || energy != game.PlayerState.Energy)
                    throw new Exception("Animation changed mechanics or did not expire.");
            }
        Prepare(1);
        var start = game.PlayerState.Position;
        game.DungeonState.Tiles[start.X + 3, start.Y] = '#';
        game.PlayerActions.Shoot(Vector2I.Right);
        if (game.VisualEffects.Actions.Animations.Single().Path[^1] != start + Vector2I.Right * 2) throw new Exception("Shot crossed a wall.");
        Prepare(2);
        game.PlayerActions.Shoot(Vector2I.Right);
        if (game.VisualEffects.Actions.Animations.Single().Path.Count != 11) throw new Exception("Missed arrow lost its range animation.");
        Prepare(1);
        game.DungeonState.Tiles[game.PlayerState.Position.X + 1, game.PlayerState.Position.Y] = '#';
        game.PlayerActions.Shoot(Vector2I.Right);
        if (game.VisualEffects.Actions.Animations.Single().Path.Count != 1) throw new Exception("Adjacent wall shot crossed its origin.");

        for (int hero = 0; hero < 4; hero++)
        {
            Prepare(hero);
            game.PlayerActions.Skill();
            if (game.VisualEffects.Actions.Active) throw new Exception("Targetless skill created an effect.");
            var target = game.PlayerState.Position + Vector2I.Right * (hero == 3 ? 1 : 2);
            game.DungeonState.Enemies.Add(new Enemy(target, 's', 1) { Health = 100, MaxHealth = 100 });
            game.PlayerState.Energy = 0;
            game.PlayerActions.Skill();
            if (game.VisualEffects.Actions.Active) throw new Exception("Failed skill created an effect.");
            game.PlayerState.Energy = game.PlayerState.MaxEnergy;
            game.PlayerActions.Skill();
            var effect = game.VisualEffects.Actions.Animations.Single(a => a.Kind == (ActionAnimationKind)hero);
            if (effect.Kind != (ActionAnimationKind)hero || effect.Targets[0] != target) throw new Exception("Wrong class or target animation.");
            for (int frame = 0; frame < 20; frame++)
            {
                if (effect.Sample().Any(g => g.Character < 32 || g.Character > 126)) throw new Exception("Non-ASCII ability effect.");
                effect.Advance(.025);
            }
            double elapsed = effect.Elapsed;
            game.RunState.Screen = "pause";
            game.Tick(.1);
            if (effect.Elapsed != elapsed) throw new Exception("Paused animation advanced.");
            game.DungeonGenerator.Generate(false);
            if (game.VisualEffects.Actions.Active) throw new Exception("Effect survived floor replacement.");
        }

        Prepare(1);
        var hidden = game.PlayerState.Position + Vector2I.Right * 2;
        game.DungeonState.Visible[hidden.X, hidden.Y] = false;
        game.VisualEffects.Actions.PlayProjectile(game.PlayerState.Position, new[] { game.PlayerState.Position, hidden }, true);
        game.DungeonState.Visible[hidden.X, hidden.Y] = true;
        var canvas = new EffectRecordingCanvas();
        var ascii = new AsciiCanvas(canvas, game.VisualEffects);
        new ActionEffectsRenderer(ascii, game.DungeonState, game.VisualEffects.Actions).Draw();
        if (canvas.Positions.Contains(new Vector2(UiTheme.MapX + hidden.X * UiTheme.CellX, UiTheme.MapY + hidden.Y * UiTheme.CellY)))
            throw new Exception("Effect revealed a tile hidden when the action began.");
        for (int i = 0; i < 50; i++) game.VisualEffects.Actions.PlayProjectile(game.PlayerState.Position, new[] { game.PlayerState.Position }, true);
        if (game.VisualEffects.Actions.Animations.Count > 24) throw new Exception("Unbounded animation accumulation.");
        game.VisualEffects.ResetEffects();
        if (game.VisualEffects.Actions.Active) throw new Exception("Reset did not clear action effects.");
        GD.Print("ACTION ANIMATION AUDIT: four skills, four shot directions, walls, range, invalid actions, ASCII, pause, fog, lifecycle and cosmetic-only timing passed.");
    }

    private sealed class EffectRecordingCanvas : IAsciiCanvas
    {
        internal readonly List<Vector2> Positions = new();
        public void DrawString(Font font, Vector2 position, string text, HorizontalAlignment alignment, float width, int fontSize, Color color) => Positions.Add(position);
        public void DrawSetTransform(Vector2 position, float rotation, Vector2 scale) { }
    }
}
