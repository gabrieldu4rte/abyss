using Godot;
using System;
using System.Collections.Generic;
namespace Abyss.Infrastructure;
internal sealed class GodotGameAudio
    : IGameAudio, IDisposable
{
    private readonly AudioStreamPlayer[] voices = new AudioStreamPlayer[10];
    private readonly AudioStreamPlayer[] music = new AudioStreamPlayer[2];
    private readonly Dictionary<string, AudioStreamWav> streams = new();
    private readonly bool persist;
    private string current = "";
    private int active, voiceIndex;
    private int musicVolume = -1, effectsVolume = -1;
    private double fade = 1;
    private const string SettingsPath = "user://audio.cfg";
    internal GodotGameAudio(Node parent, bool persist)
    {
        this.persist = persist;
        for (int i = 0; i < voices.Length; i++) { voices[i] = new AudioStreamPlayer(); parent.AddChild(voices[i]); }
        for (int i = 0; i < music.Length; i++) { music[i] = new AudioStreamPlayer(); parent.AddChild(music[i]); }
    }
    internal void LoadSettings(MenuState menu)
    {
        if (!persist) return;
        var config = new ConfigFile();
        if (config.Load(SettingsPath) != Error.Ok) return;
        menu.MusicVolume = Math.Clamp(config.GetValue("audio", "music", 50).AsInt32(), 0, 100);
        menu.EffectsVolume = Math.Clamp(config.GetValue("audio", "effects", 75).AsInt32(), 0, 100);
    }
    private AudioStreamWav Load(string key, bool loop)
    {
        if (streams.TryGetValue(key, out var cached)) return cached;
        // Generator writes canonical mono, PCM16, 22050 Hz WAVs with a 44-byte header.
        byte[] wav = FileAccess.GetFileAsBytes("res://Audio/" + key + ".wav");
        if (wav.Length < 44) throw new InvalidOperationException("Missing audio asset: " + key);
        var stream = new AudioStreamWav {
            Format = AudioStreamWav.FormatEnum.Format16Bits, MixRate = 22050, Stereo = false,
            Data = wav[44..], LoopMode = loop ? AudioStreamWav.LoopModeEnum.Forward : AudioStreamWav.LoopModeEnum.Disabled,
            LoopBegin = 0, LoopEnd = (wav.Length - 44) / 2
        };
        streams[key] = stream;
        return stream;
    }
    public void Play(string cue)
    {
        if (effectsVolume <= 0) return;
        var voice = voices[voiceIndex++ % voices.Length];
        voice.Stop(); voice.Stream = Load(cue, false);
        voice.VolumeDb = Mathf.LinearToDb(effectsVolume / 100f * .42f);
        voice.Play();
    }
    public void SetMusic(string track)
    {
        if (current == track) return;
        current = track;
        active = 1 - active;
        music[active].Stop(); music[active].Stream = Load("music_" + track, true);
        music[active].VolumeDb = -80;
        music[active].Play(); fade = 0;
    }
    public void SetVolumes(int musicPercent, int effectsPercent)
    {
        if (musicVolume == musicPercent && effectsVolume == effectsPercent) return;
        musicVolume = musicPercent; effectsVolume = effectsPercent;
        foreach (var voice in voices) voice.VolumeDb = effectsVolume == 0 ? -80 : Mathf.LinearToDb(effectsVolume / 100f * .42f);
        if (!persist) return;
        var config = new ConfigFile(); config.SetValue("audio", "music", musicVolume); config.SetValue("audio", "effects", effectsVolume);
        if (config.Save(SettingsPath) != Error.Ok) GD.PushWarning("Could not save audio preferences.");
    }
    public void Dispose()
    {
        foreach (var voice in voices) { voice.Stop(); voice.Stream = null; }
        foreach (var player in music) { player.Stop(); player.Stream = null; }
        foreach (var stream in streams.Values) stream.Dispose();
        streams.Clear();
    }
    public void Advance(double delta)
    {
        fade = Math.Min(1, fade + delta / 1.8);
        for (int i = 0; i < music.Length; i++)
        {
            float gain = musicVolume / 100f * (float)(i == active ? fade : 1 - fade);
            music[i].VolumeDb = gain <= 0 ? -80 : Mathf.LinearToDb(gain);
        }
        if (fade >= 1) music[1 - active].Stop();
    }
}
