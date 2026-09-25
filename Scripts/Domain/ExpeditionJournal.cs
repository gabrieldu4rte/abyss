using System.Collections.Generic;

namespace Abyss.Domain;
internal sealed class ExpeditionJournal
{
    internal int LastPotionRoll { get; set; }
    internal int LastPotionHealing { get; set; }
    internal string LastRollPt { get; set; } = "";
    internal string LastRollEn { get; set; } = "";
    internal List<(string Pt, string En)> Entries { get; } = new();

    internal void Say(string pt, string en)
    {
        Entries.Insert(0, (pt, en));
    }
}
