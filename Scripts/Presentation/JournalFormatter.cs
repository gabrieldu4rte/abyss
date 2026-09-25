using System.Collections.Generic;

namespace Abyss.Presentation;
internal sealed class JournalFormatter
{
    private readonly ExpeditionJournal expeditionJournal;
    private readonly Localization localization;
    internal JournalFormatter(ExpeditionJournal expeditionJournal, Localization localization)
    {
        this.expeditionJournal = expeditionJournal;
        this.localization = localization;
    }

    internal List<string> JournalLines()
    {
        var lines = new List<string>();
        foreach (var entry in expeditionJournal.Entries)
        {
            string remaining = "> " + localization.Translate(entry.Pt, entry.En);
            while (remaining.Length > 91)
            {
                int cut = remaining.LastIndexOf(' ', 90);
                if (cut < 3)
                    cut = 91;
                lines.Add(remaining[..cut]);
                remaining = "  " + remaining[cut..].TrimStart();
            }

            lines.Add(remaining);
        }

        return lines;
    }
}
