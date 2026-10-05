using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
namespace Abyss.Tests;
internal sealed partial class RegressionSuite
{
    internal void TestAudio()
    {
        void Check(bool ok, string why) { if (!ok) throw new Exception(why); }
        var output = new RecordingAudio();
        var g = new GameSession(new TestHost(), new TestCanvas(), new TestSettings(), null, output);
        g.Start(918); g.DungeonState.Enemies.Clear(); g.DungeonState.Items.Clear(); g.DungeonState.Environment.Clear();
        g.DungeonState.Environment.Biome = Biome.Ruins;
        g.PlayerState.Position = new Vector2I(25, 13); g.DungeonState.Tiles[25,13] = g.DungeonState.Tiles[26,13] = '.';
        g.DungeonState.Tiles[24,13] = '#';
        g.AudioController.Update(.01); output.Cues.Clear();
        g.PlayerActions.Move(Vector2I.Left); g.AudioController.Update(.01);
        Check(!output.Cues.Contains("step"), "Wall collision played a footstep.");
        g.PlayerActions.Move(Vector2I.Right); g.AudioController.Update(.01);
        Check(output.Cues.Count(c => c == "step") == 1, "Successful movement lacks one footstep.");
        int turn = g.RunState.Turn, health = g.PlayerState.Health;
        g.AudioController.Update(10);
        Check(g.RunState.Turn == turn && g.PlayerState.Health == health, "Audio changed gameplay.");
        foreach (var kind in Enum.GetValues<ActionAnimationKind>())
        {
            g.VisualEffects.Sounds.Play(kind.ToString().ToLowerInvariant());
        }
        g.AudioController.Update(.01);
        Check(output.Cues.Contains("arcanenova") && output.Cues.Contains("furnacecross"), "Ability audio missing.");
        for (int b = 0; b < 4; b++)
        {
            g.DungeonState.Environment.Biome = (Biome)b; g.AudioController.Update(.01);
            Check(output.Track == new[] { "ruins", "cistern", "fungal", "forge" }[b], "Wrong biome score.");
        }
        g.RunState.Screen = "pause"; g.AudioController.Update(.01);
        Check(output.Track == "forge", "Pause restarted menu music.");
        g.GameInput.HandleKey(Key.F7); g.GameInput.HandleKey(Key.F8); g.AudioController.Update(.01);
        Check(output.Music == 75 && output.Effects == 100 && g.RunState.Turn == turn, "Volume shortcuts consume actions or failed.");
        g.RunState.Screen = "dead"; g.AudioController.Update(.01); g.AudioController.Update(.01);
        Check(output.Cues.Count(c => c == "death") == 1, "Death cue repeats or is absent.");
        using var dir = DirAccess.Open("res://Audio");
        int files = 0;
        foreach (string name in dir.GetFiles().Select(n => n.EndsWith(".wav.import") ? n[..^7] : n).Where(n => n.EndsWith(".wav")).Distinct())
        {
            string path = "res://Audio/" + name;
            var stream = GD.Load<AudioStreamWav>(path);
            Check(stream != null && stream.GetLength() > 0 && stream.MixRate == 22050 && !stream.Stereo, "Missing or invalid imported audio: " + name);
            files++;
            // Exported builds contain imported resources, not the original PCM files.
            if (!Godot.FileAccess.FileExists(path)) continue;
            var data = Godot.FileAccess.GetFileAsBytes(path);
            Check(data.Length > 44 && System.Text.Encoding.ASCII.GetString(data, 0, 4) == "RIFF" && BitConverter.ToInt32(data,24) == 22050 && BitConverter.ToInt16(data,22) == 1 && BitConverter.ToInt16(data,34) == 16, "Unexpected audio encoding.");
            int peak = 0;
            for (int i = 44; i < data.Length; i += 2) peak = Math.Max(peak, Math.Abs((int)BitConverter.ToInt16(data,i)));
            Check(peak > 100 && peak < 30000, "Silent or clipped audio asset.");
        }
        Check(files == 35, "Incomplete sound library.");
        GD.Print("AUDIO AUDIT: 28 short effects, seven loops, imported resources, source PCM validation when available, movement, cue routing, biome music, pause, death, volume and cosmetic-only playback passed.");
    }
    private sealed class RecordingAudio : IGameAudio
    {
        internal readonly List<string> Cues = new();
        internal string Track = "";
        internal int Music, Effects;
        public void Play(string cue) => Cues.Add(cue);
        public void SetMusic(string track) => Track = track;
        public void SetVolumes(int music, int effects) { Music = music; Effects = effects; }
        public void Advance(double delta) { }
    }
}
