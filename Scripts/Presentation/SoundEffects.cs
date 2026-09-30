using System.Collections.Generic;
namespace Abyss.Presentation;
internal sealed class SoundEffects
{
    private readonly Queue<string> pending = new();
    internal void Play(string cue) { if (pending.Count < 64) pending.Enqueue(cue); }
    internal IEnumerable<string> Drain() { while (pending.TryDequeue(out var cue)) yield return cue; }
    internal void Clear() => pending.Clear();
}
