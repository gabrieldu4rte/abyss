using Godot;
using System;
using System.Collections.Generic;

namespace Abyss.Tests;
internal sealed partial class RegressionSuite
{
    internal void TestScreenTransitions()
    {
        var isolated = new GameSession(new TestHost(), new TestCanvas(), new TestSettings());
        foreach (bool english in new[] { false, true })
        {
            isolated.MenuState.English = english;
            isolated.OpeningStory.Begin();
            if (isolated.OpeningStory.VisibleCharacters != 0) throw new Exception("Opening must start empty.");
            isolated.Tick(.5);
            if (isolated.OpeningStory.VisibleCharacters != 21 || isolated.RunState.Turn != 0)
                throw new Exception("Typewriter timing changed game turns.");
            isolated.GameInput.HandleKey(Key.Enter);
            if (!isolated.OpeningStory.Complete || isolated.RunState.Screen != "intro")
                throw new Exception("First confirmation must reveal the story.");
            isolated.GameInput.HandleKey(Key.Enter);
            if (isolated.RunState.Screen != "home") throw new Exception("Completed intro did not continue.");
            isolated.OpeningStory.Begin();
            isolated.GameInput.HandleKey(Key.Escape);
            if (isolated.RunState.Screen != "home") throw new Exception("Intro cannot be skipped.");
            isolated.OpeningStory.Begin();
            isolated.Tick(20);
            if (isolated.RunState.Screen != "home") throw new Exception("Intro did not finish automatically.");
        }
        var sink = new FadeTestCanvas();
        var fade = new ScreenTransitions(sink) { Enabled = true };
        void Render(string route)
        {
            sink.Glyphs.Clear();
            fade.BeginFrame(route);
            fade.DrawString(null!, Vector2.Zero, route, HorizontalAlignment.Left, -1, 18, Colors.White);
            fade.EndFrame();
        }
        foreach (var pair in new[] { ("intro", "home"), ("home", "classes"), ("classes", "game"), ("game", "pause"), ("pause", "home") })
        {
            fade.Advance(1);
            Render(pair.Item1);
            fade.Advance(1);
            Render(pair.Item1);
            Render(pair.Item2);
            if (sink.Glyphs[0] != (pair.Item1, 1f)) throw new Exception("Transition lost the outgoing frame.");
            fade.Advance(.09);
            Render(pair.Item2);
            if (sink.Glyphs[0].Text != pair.Item1 || Math.Abs(sink.Glyphs[0].Alpha - .5f) > .001)
                throw new Exception("Outgoing glyphs did not fade.");
            fade.Advance(.18);
            Render(pair.Item2);
            if (sink.Glyphs[0].Text != pair.Item2 || Math.Abs(sink.Glyphs[0].Alpha - .5f) > .001)
                throw new Exception("Incoming glyphs did not fade.");
            fade.Advance(1);
            Render(pair.Item2);
            if (fade.Active || sink.Glyphs[0] != (pair.Item2, 1f)) throw new Exception("Fade did not finish at full opacity.");
        }
        isolated.Start(520);
        isolated.Transitions.Enabled = true;
        isolated.Transitions.BeginFrame("home");
        isolated.Transitions.BeginFrame("game");
        int turn = isolated.RunState.Turn;
        isolated.GameInput.HandleEvent(new InputEventKey { Pressed = true, Keycode = Key.Space });
        isolated.GameInput.HeldMovementKey = Key.Right;
        isolated.GameInput.AdvanceHeldMovement(1);
        if (isolated.RunState.Turn != turn || isolated.GameInput.HeldMovementKey != Key.None)
            throw new Exception("Input consumed a turn during a transition.");
        GD.Print("SCREEN FLOW AUDIT: bilingual typewriter, skip, automatic continuation, two-way fades, and input gating passed.");
    }
    private sealed class FadeTestCanvas : IAsciiCanvas
    {
        internal List<(string Text, float Alpha)> Glyphs { get; } = new();
        public void DrawString(Font font, Vector2 position, string text, HorizontalAlignment alignment, float width, int fontSize, Color color) => Glyphs.Add((text, color.A));
        public void DrawSetTransform(Vector2 position, float rotation, Vector2 scale) { }
    }
}
