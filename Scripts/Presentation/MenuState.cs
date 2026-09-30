namespace Abyss.Presentation;
internal sealed class MenuState
{
    internal int MusicVolume { get; set; } = 50;
    internal int EffectsVolume { get; set; } = 75;
    internal int PauseTab { get; set; }
    internal int JournalPage { get; set; }
    internal bool English { get; set; }
    internal bool IsTesting { get; set; }
    internal int MenuIndex { get; set; }
    internal int LanguageIndex { get; set; }
    internal string LanguageReturn { get; set; } = "home";
    internal string LanguageNotice { get; set; } = "";
    internal bool BestiaryOpen { get; set; }
    internal int BestiaryBiome { get; set; }
    internal int BestiaryEntry { get; set; }
    internal int HelpTopic { get; set; }
    internal bool ShopSelling { get; set; }
    internal bool ConfirmYes { get; set; }
    internal bool ExitYes { get; set; }
    internal int ShopIndex { get; set; }
    internal int MerchantQuote { get; set; }
    internal Offer? PendingTrade { get; set; }
    internal (string Pt, string En) ShopNotice { get; set; } = ("", "");
    internal int InventoryIndex { get; set; }
    internal (string Pt, string En) InventoryNotice { get; set; } = ("", "");
}
