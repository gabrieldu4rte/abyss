using Godot;
using System;

namespace Abyss.Presentation;
internal sealed class PauseRenderer
{
    private readonly AsciiCanvas asciiCanvas;
    private readonly DungeonState dungeonState;
    private readonly ExpeditionJournal expeditionJournal;
    private readonly HeroCombatStats heroCombatStats;
    private readonly InventoryRenderer inventoryRenderer;
    private readonly InventoryState inventoryState;
    private readonly JournalFormatter journalFormatter;
    private readonly Localization localization;
    private readonly MenuState menuState;
    private readonly PlayerState playerState;
    private readonly UiComponents uiComponents;
    internal PauseRenderer(AsciiCanvas asciiCanvas, DungeonState dungeonState, ExpeditionJournal expeditionJournal, HeroCombatStats heroCombatStats, InventoryRenderer inventoryRenderer, InventoryState inventoryState, JournalFormatter journalFormatter, Localization localization, MenuState menuState, PlayerState playerState, UiComponents uiComponents)
    {
        this.asciiCanvas = asciiCanvas;
        this.dungeonState = dungeonState;
        this.expeditionJournal = expeditionJournal;
        this.heroCombatStats = heroCombatStats;
        this.inventoryRenderer = inventoryRenderer;
        this.inventoryState = inventoryState;
        this.journalFormatter = journalFormatter;
        this.localization = localization;
        this.menuState = menuState;
        this.playerState = playerState;
        this.uiComponents = uiComponents;
    }

    internal void DrawPause()
    {
        asciiCanvas.Text(32, 95, localization.Translate("PAUSADO", "PAUSED"), UiTheme.Gold, 22);
        string[] tabs =
        {
            localization.Translate("[1] PERSONAGEM", "[1] CHARACTER"),
            localization.Translate("[2] INVENTARIO", "[2] INVENTORY"),
            localization.Translate("[3] DIARIO", "[3] JOURNAL"),
            localization.Translate("[4] AJUDA", "[4] HELP"),
            localization.Translate("[5] CONFIGURACOES", "[5] SETTINGS")
        };
        for (int i = 0; i < 5; i++)
            asciiCanvas.Text(32 + i * 248, 143, (menuState.PauseTab == i ? "> " : "  ") + tabs[i], menuState.PauseTab == i ? UiTheme.Teal : UiTheme.Dim, 16);
        asciiCanvas.Rule(163);
        if (menuState.PauseTab == 0)
            DrawCharacterSheet();
        else if (menuState.PauseTab == 2)
            DrawJournal();
        else if (menuState.PauseTab == 3)
            DrawHelpTopics();
        else if (menuState.PauseTab == 4)
            DrawPauseSettings();
        else
            inventoryRenderer.DrawInventory();
        uiComponents.Footer("[1-5 / A D / TAB] abas   [CIMA/BAIXO] navegar   [ENTER] confirmar   [ESC] continuar", "[1-5 / A D / TAB] tabs   [UP/DOWN] navigate   [ENTER] confirm   [ESC] resume");
    }

    internal void DrawCharacterSheet()
    {
        asciiCanvas.Portrait(32, 202, AsciiArt.Heroes[playerState.ClassIndex], localization.ClassName(playerState.ClassIndex), UiTheme.Teal);
        asciiCanvas.Text(32, 530, localization.Translate($"NIVEL {playerState.Level} / ANDAR {dungeonState.Floor}", $"LEVEL {playerState.Level} / FLOOR {dungeonState.Floor}"), UiTheme.Gold, 18);
        asciiCanvas.Text(32, 563, $"XP {playerState.Experience} / {heroCombatStats.XpToNext}", UiTheme.Ink, 17);
        asciiCanvas.Text(32, 596, localization.Translate($"DERROTADOS {playerState.Kills}", $"DEFEATED {playerState.Kills}"), UiTheme.Dim, 16);
        asciiCanvas.Text(310, 208, localization.Translate("ATRIBUTOS / MODIFICADORES", "ATTRIBUTES / MODIFIERS"), UiTheme.Gold, 18);
        asciiCanvas.Lines(310, 245, localization.Translate($"FORCA         {heroCombatStats.EffectiveAttributes.Strength, 2}  ({UiTheme.Signed(heroCombatStats.EffectiveAttributes.Str)})\nDESTREZA      {heroCombatStats.EffectiveAttributes.Dexterity, 2}  ({UiTheme.Signed(heroCombatStats.EffectiveAttributes.Dex)})\nCONSTITUICAO  {heroCombatStats.EffectiveAttributes.Constitution, 2}  ({UiTheme.Signed(heroCombatStats.EffectiveAttributes.Con)})\nINTELIGENCIA  {heroCombatStats.EffectiveAttributes.Intelligence, 2}  ({UiTheme.Signed(heroCombatStats.EffectiveAttributes.Int)})", $"STRENGTH      {heroCombatStats.EffectiveAttributes.Strength, 2}  ({UiTheme.Signed(heroCombatStats.EffectiveAttributes.Str)})\nDEXTERITY     {heroCombatStats.EffectiveAttributes.Dexterity, 2}  ({UiTheme.Signed(heroCombatStats.EffectiveAttributes.Dex)})\nCONSTITUTION  {heroCombatStats.EffectiveAttributes.Constitution, 2}  ({UiTheme.Signed(heroCombatStats.EffectiveAttributes.Con)})\nINTELLIGENCE  {heroCombatStats.EffectiveAttributes.Intelligence, 2}  ({UiTheme.Signed(heroCombatStats.EffectiveAttributes.Int)})"), UiTheme.Ink, 17, 29);
        asciiCanvas.Text(755, 208, localization.Translate("RECURSOS E DEFESAS", "RESOURCES AND DEFENSES"), UiTheme.Gold, 18);
        asciiCanvas.Lines(755, 245, localization.Translate($"Vida {playerState.Health}/{playerState.MaxHealth}   Energia {playerState.Energy}/{playerState.MaxEnergy}\nPocoes {inventoryState.Potions}   Ouro {playerState.Gold}\nDefesa {heroCombatStats.Defense}\nProficiencia {UiTheme.Signed(heroCombatStats.Proficiency)}", $"Health {playerState.Health}/{playerState.MaxHealth}   Energy {playerState.Energy}/{playerState.MaxEnergy}\nPotions {inventoryState.Potions}   Gold {playerState.Gold}\nDefense {heroCombatStats.Defense}\nProficiency {UiTheme.Signed(heroCombatStats.Proficiency)}"), UiTheme.Ink, 16, 29);
        asciiCanvas.Text(310, 393, localization.Translate("ATAQUES E HABILIDADES", "ATTACKS AND ABILITIES"), UiTheme.Gold, 18);
        asciiCanvas.Text(310, 426, !heroCombatStats.CanMelee ? localization.Translate("Sem ataque corpo a corpo. Use [F] ou [Q].", "No melee attack. Use [F] or [Q].") : localization.Translate($"Basico adjacente: d20{UiTheme.Signed(heroCombatStats.MeleeBonus)} | Dano {heroCombatStats.MeleeDice}", $"Adjacent basic: d20{UiTheme.Signed(heroCombatStats.MeleeBonus)} | Damage {heroCombatStats.MeleeDice}"), UiTheme.Ink, 17);
        asciiCanvas.Text(310, 458, $"[Q] {localization.SkillName(playerState.ClassIndex)} | {heroCombatStats.AbilityCost} EN | d20{UiTheme.Signed(heroCombatStats.SpellBonus + 2)} | {heroCombatStats.AbilityDice}", UiTheme.Teal, 17);
        asciiCanvas.Text(310, 487, localization.Translate($"Alcance: {heroCombatStats.AbilityRange} casas", $"Range: {heroCombatStats.AbilityRange} tiles") + (playerState.ClassIndex == 3 ? localization.Translate(" | Critico 19-20; +4 defesa na resposta", " | Critical 19-20; +4 defense on response") : ""), UiTheme.Dim, 15);
        if (playerState.ClassIndex is 1 or 2)
            asciiCanvas.Text(310, 519, heroCombatStats.CanShoot ? localization.Translate($"[F] Basico: {heroCombatStats.ShotCost} EN | d20{UiTheme.Signed(heroCombatStats.SpellBonus)} | {heroCombatStats.ShotDice} | Alcance {(playerState.ClassIndex == 2 ? 10 : 6)}", $"[F] Basic: {heroCombatStats.ShotCost} EN | d20{UiTheme.Signed(heroCombatStats.SpellBonus)} | {heroCombatStats.ShotDice} | Range {(playerState.ClassIndex == 2 ? 10 : 6)}") : localization.Translate("[F] Equipe um arco/cajado compativel para disparar.", "[F] Equip a compatible bow/staff to shoot."), UiTheme.Ink, 16);
        asciiCanvas.Text(310, 551, localization.Translate("[P] Pocao: cura 2d10 (2-20 PV), consome uma acao.", "[P] Potion: heals 2d10 (2-20 HP), uses one action."), UiTheme.Ink, 16);
        asciiCanvas.Text(310, 603, localization.Translate("EQUIPAMENTOS", "EQUIPMENT"), UiTheme.Gold, 16);
        for (int i = 0; i < 3; i++)
            asciiCanvas.Text(310, 630 + i * 22, localization.SlotName(i) + ": " + (inventoryState.Equipped[i] is Gear g ? localization.GearLabel(g) : localization.Translate("Vazio", "Empty")), inventoryState.Equipped[i] is Gear gear ? UiTheme.RarityColor(gear.Quality) : UiTheme.Dim, 15);
    }

    internal void DrawJournal()
    {
        asciiCanvas.DrawAsciiImage(32, 215, AsciiArt.Book, 245, 280, new Color("fff6df"), 3);
        asciiCanvas.Text(32, 540, localization.Translate("DIARIO DA EXPEDICAO", "EXPEDITION JOURNAL"), UiTheme.Gold, 16);
        asciiCanvas.Lines(32, 577, localization.Translate("Mais recentes primeiro.\n[CIMA/BAIXO] paginas\n[HOME/END] inicio/fim", "Newest entries first.\n[UP/DOWN] pages\n[HOME/END] first/last"), UiTheme.Dim, 14, 27);
        var lines = journalFormatter.JournalLines();
        int pages = Math.Max(1, (lines.Count + UiTheme.JournalPageSize - 1) / UiTheme.JournalPageSize);
        menuState.JournalPage = Math.Clamp(menuState.JournalPage, 0, pages - 1);
        asciiCanvas.Text(310, 208, localization.Translate($"PAGINA {menuState.JournalPage + 1}/{pages} / {expeditionJournal.Entries.Count} REGISTROS", $"PAGE {menuState.JournalPage + 1}/{pages} / {expeditionJournal.Entries.Count} ENTRIES"), UiTheme.Gold, 17);
        if (lines.Count == 0)
            asciiCanvas.Text(310, 252, localization.Translate("Nenhum registro nesta expedicao.", "No entries in this expedition."), UiTheme.Dim, 16);
        for (int i = 0; i < UiTheme.JournalPageSize && menuState.JournalPage * UiTheme.JournalPageSize + i < lines.Count; i++)
            asciiCanvas.Text(310, 250 + i * 25, lines[menuState.JournalPage * UiTheme.JournalPageSize + i], i % 2 == 0 ? UiTheme.Ink : UiTheme.Dim, 16);
    }

    internal void DrawPauseSettings()
    {
        asciiCanvas.DrawAsciiImage(32, 200, AsciiArt.Camp, 490, 470, new Color("fff6df"), 2);
        asciiCanvas.Text(600, 233, localization.Translate("CONFIGURACOES DA EXPEDICAO", "EXPEDITION SETTINGS"), UiTheme.Gold, 24);
        uiComponents.MenuItem(600, 300, 0, localization.Translate("IDIOMA", "LANGUAGE"));
        uiComponents.MenuItem(600, 340, 1, localization.Translate("VOLTAR AO MENU PRINCIPAL", "RETURN TO MAIN MENU"));
    }

    internal void DrawHelpTopics()
    {
        string[] topics =
        {
            localization.Translate("O ABISMO", "THE ABYSS"),
            localization.Translate("CAMINHOS E REFUGIOS", "PATHS AND REFUGES"),
            localization.Translate("HABITANTES", "INHABITANTS"),
            localization.Translate("VOCACOES", "CALLINGS"),
            localization.Translate("EQUIPAMENTOS E RELIQUIAS", "EQUIPMENT AND RELICS"),
            localization.Translate("PROVISOES", "SUPPLIES"),
            localization.Translate("O MERCADOR", "THE MERCHANT")
        };
        asciiCanvas.Text(32, 206, localization.Translate("COMPENDIO DO VIAJANTE", "TRAVELER'S COMPENDIUM"), UiTheme.Gold, 17);
        for (int i = 0; i < topics.Length; i++)
            asciiCanvas.Text(32, 251 + i * 38, (menuState.HelpTopic == i ? "> " : "  ") + topics[i], menuState.HelpTopic == i ? UiTheme.Teal : UiTheme.Dim, 15);
        asciiCanvas.Text(32, 572, localization.Translate("[CIMA/BAIXO] categoria", "[UP/DOWN] category"), UiTheme.Dim, 14);
        float y = 212;
        foreach (string line in localization.HelpText(menuState.HelpTopic).Split('\n'))
        {
            asciiCanvas.Text(355, y, line.StartsWith("# ") ? line[2..] : line, line.StartsWith("# ") ? UiTheme.Gold : UiTheme.Ink, 16);
            y += 29;
        }
    }
}
