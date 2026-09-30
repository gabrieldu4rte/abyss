using System.Collections.Generic;
namespace Abyss.Application;
internal sealed class BestiaryProgress
{
    private readonly BestiaryState state;
    private readonly IBestiaryStore? store;
    internal BestiaryProgress(BestiaryState state, IBestiaryStore? store)
    {
        this.state = state;
        this.store = store;
        if (store == null) return;
        var valid = new HashSet<string>();
        for (int biome = 0; biome < 4; biome++)
            foreach (char glyph in EnemyCatalog.Roster(biome * 5 + 1) + "B")
                valid.Add(EnemyCatalog.Get(glyph, biome * 5 + 1).ArtKey);
        foreach (var entry in store.Load())
            if (valid.Contains(entry.Key) && entry.Value > 0) state.Restore(entry.Key, entry.Value);
    }
    internal bool Record(Enemy enemy)
    {
        state.Record(enemy.Profile.ArtKey);
        return store?.Save(state.Defeated) ?? true;
    }
}
