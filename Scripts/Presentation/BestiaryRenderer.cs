using System.Collections.Generic;
namespace Abyss.Presentation;
internal sealed class BestiaryRenderer(AsciiCanvas canvas, BestiaryState bestiary, MenuState menu, Localization text)
{
    internal void Draw()
    {
        int depth = menu.BestiaryBiome * 5 + 1;
        string roster = EnemyCatalog.Roster(depth) + "B";
        canvas.Text(32, 206, text.Translate("BESTIARIO", "BESTIARY"), UiTheme.Gold, 20);
        canvas.Text(355, 206, EnvironmentAppearance.Name((Biome)menu.BestiaryBiome, text), UiTheme.Teal, 18);
        canvas.Text(355, 237, text.Translate("[ESQUERDA/DIREITA] bioma", "[LEFT/RIGHT] biome"), UiTheme.Dim, 14);
        for (int i = 0; i < roster.Length; i++)
        {
            string key = EnemyCatalog.Get(roster[i], depth).ArtKey;
            bool known = bestiary.Count(key) > 0;
            string label = known ? EnemyText.Name(roster[i], depth, text.English) : "???";
            canvas.Text(32, 280 + i * 52, (menu.BestiaryEntry == i ? "> " : "  ") + label, menu.BestiaryEntry == i ? UiTheme.Teal : known ? UiTheme.Ink : UiTheme.Dim, 16);
        }
        var profile = EnemyCatalog.Get(roster[menu.BestiaryEntry], depth);
        int count = bestiary.Count(profile.ArtKey);
        bool unlocked = count > 0;
        string name = unlocked ? EnemyText.Name(roster[menu.BestiaryEntry], depth, text.English) : "???";
        canvas.Portrait(355, 280, unlocked ? AsciiArt.Creature(profile.ArtKey) : AsciiArt.Unknown, "", unlocked ? UiTheme.Gold : UiTheme.Dim);
        canvas.Text(615, 292, name.ToUpperInvariant(), unlocked ? UiTheme.Gold : UiTheme.Dim, 21);
        canvas.Text(615, 330, text.Translate($"DERROTADOS: {count}", $"DEFEATED: {count}"), UiTheme.Teal, 16);
        string lore = unlocked ? BestiaryLore.Description(profile.ArtKey, text.English)
            : text.Translate("Esta pagina ainda nao foi escrita. Derrote esta criatura para revelar sua historia.", "This page has not yet been written. Defeat this creature to reveal its story.");
        int row = 0;
        foreach (string line in Wrap(lore, 56)) canvas.Text(615, 382 + row++ * 27, line, UiTheme.Ink, 16);
        canvas.Text(32, 590, text.Translate("[CIMA/BAIXO] criatura", "[UP/DOWN] creature"), UiTheme.Dim, 14);
        canvas.Text(32, 620, text.Translate("[ESC] voltar ao compendio", "[ESC] back to compendium"), UiTheme.Dim, 14);
    }
    private static IEnumerable<string> Wrap(string text, int width)
    {
        string line = "";
        foreach (string word in text.Split(' '))
        {
            if (line.Length > 0 && line.Length + word.Length + 1 > width) { yield return line; line = ""; }
            line += (line.Length > 0 ? " " : "") + word;
        }
        if (line.Length > 0) yield return line;
    }
}
