using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

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
        fade.Advance(1);
        Render("game");
        fade.Advance(1);
        Render("game");
        bool footstepsPlaying = true;
        fade.BeginFloorChange(() => footstepsPlaying);
        if (!fade.Active) throw new Exception("Floor fade did not gate input immediately.");
        fade.Advance(.2); Render("game");
        if (Math.Abs(sink.Glyphs[0].Alpha - .5f) > .001) throw new Exception("Floor fade-out timing failed.");
        fade.Advance(.2); Render("game");
        if (sink.Glyphs[0].Alpha > .001) throw new Exception("Floor transition did not reach black.");
        fade.Advance(.5); Render("game");
        if (sink.Glyphs[0].Alpha > .001 || !fade.Active) throw new Exception("Floor appeared before footsteps finished.");
        footstepsPlaying = false;
        fade.Advance(.2); Render("game");
        if (Math.Abs(sink.Glyphs[0].Alpha - .5f) > .001) throw new Exception("Floor fade-in timing failed.");
        fade.Advance(.21); Render("game");
        if (fade.Active || sink.Glyphs[0].Alpha != 1) throw new Exception("Floor fade did not finish.");
        var audio = new RecordingAudio();
        var descent = new GameSession(new TestHost(), new TestCanvas(), new TestSettings(), null, audio);
        descent.Start(812);
        descent.Transitions.Enabled = true;
        descent.Transitions.BeginFrame("game"); descent.Transitions.EndFrame();
        descent.PlayerState.Position = descent.DungeonState.Stairs;
        descent.Descend();
        if (descent.DungeonState.Floor != 2 || !descent.Transitions.Active) throw new Exception("Successful descent lacks fade.");
        descent.PlayerState.Position = descent.DungeonState.Stairs;
        descent.Descend();
        if (descent.DungeonState.Floor != 2) throw new Exception("Repeated input descended during fade.");
        for (int i = 0; i < 9; i++) descent.Tick(.1);
        if (audio.Cues.Count(c => c == "step") != 3 || descent.RunState.Turn != 0 || !descent.Transitions.Active || !descent.AudioController.DescentPlaying)
            throw new Exception("Transition did not wait for the last footstep tail.");
        descent.Tick(.03);
        if (!descent.AudioController.DescentPlaying || !descent.Transitions.Active) throw new Exception("Last footstep was cut short.");
        descent.Tick(.5);
        if (descent.Transitions.Active || descent.AudioController.DescentPlaying) throw new Exception("Descent did not finish after the last footstep.");
        audio.Cues.Clear();
        descent.PlayerState.Position = Godot.Vector2I.Zero;
        descent.Descend(); descent.Tick(1);
        if (audio.Cues.Contains("step") || descent.Transitions.Active) throw new Exception("Failed descent triggered effects.");
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
