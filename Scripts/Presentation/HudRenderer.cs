using Godot;
using System;
using System.Linq;

namespace Abyss.Presentation;
internal sealed class HudRenderer
{
    private readonly AsciiCanvas asciiCanvas;
    private readonly ActionEffectsRenderer actionEffectsRenderer;
    private readonly DungeonState dungeonState;
    private readonly ExpeditionJournal expeditionJournal;
    private readonly HeroCombatStats heroCombatStats;
    private readonly InventoryState inventoryState;
    private readonly Localization localization;
    private readonly PlayerState playerState;
    private readonly RunState runState;
    private readonly VisualEffects visualEffects;
    internal HudRenderer(AsciiCanvas asciiCanvas, DungeonState dungeonState, ExpeditionJournal expeditionJournal, HeroCombatStats heroCombatStats, InventoryState inventoryState, Localization localization, PlayerState playerState, RunState runState, VisualEffects visualEffects, ActionEffectsRenderer actionEffectsRenderer)
    {
        this.asciiCanvas = asciiCanvas;
        this.actionEffectsRenderer = actionEffectsRenderer;
        this.dungeonState = dungeonState;
        this.expeditionJournal = expeditionJournal;
        this.heroCombatStats = heroCombatStats;
        this.inventoryState = inventoryState;
        this.localization = localization;
        this.playerState = playerState;
        this.runState = runState;
        this.visualEffects = visualEffects;
    }

    internal void DrawGame()
    {
        asciiCanvas.Text(32, 91, localization.Translate($"ANDAR {dungeonState.Floor}", $"FLOOR {dungeonState.Floor}"), UiTheme.Gold, 18);
        asciiCanvas.Text(272, 91, EnvironmentAppearance.Name(dungeonState.Environment.Biome, localization), UiTheme.Teal, 16);
        bool showBlacksmith = dungeonState.IsSanctuary(playerState.Position) && dungeonState.Visible[dungeonState.BlacksmithPosition.X, dungeonState.BlacksmithPosition.Y];
        var target = showBlacksmith ? null : visualEffects.FocusEnemy();
        if (target?.IsElite == true && !dungeonState.IsMerchantFloor)
        {
            for (int i = 0; i < target.Titles.Length; i++)
                asciiCanvas.Text(1012, 85 + i * 20, (i == 0 ? "ELITE / " : "") + FloorEventText.Title(target.Titles[i], localization.English), FloorEventText.TitleColor(target.Titles[i]), 14);
        }
        else
            asciiCanvas.Text(1012, 91, showBlacksmith ? localization.Translate("FERREIRO PERDIDO", "LOST BLACKSMITH") : dungeonState.IsMerchantFloor ? localization.Translate("MERCADOR", "MERCHANT") : localization.Translate("ALVO SELECIONADO", "SELECTED TARGET"), showBlacksmith || dungeonState.IsMerchantFloor ? UiTheme.Gold : UiTheme.Red, 16);
        if (dungeonState.Modifier != FloorModifier.None)
            asciiCanvas.Text(650, 91, FloorEventText.Name(dungeonState.Modifier, localization.English), UiTheme.Gold, 13);
        asciiCanvas.Portrait(24, 121, HeroPortrait.Select(playerState), "@ " + localization.HeroName(playerState), UiTheme.Teal, visualEffects.HeroHurtRemaining, visualEffects.HeroDamage, StatusText.For(playerState, null, dungeonState, localization));
        asciiCanvas.Portrait(1012, 121, showBlacksmith ? AsciiArt.Blacksmith : dungeonState.IsMerchantFloor ? AsciiArt.Merchant : AsciiArt.Enemy(target), showBlacksmith ? localization.Translate("FERREIRO PERDIDO", "LOST BLACKSMITH") : dungeonState.IsMerchantFloor ? localization.Translate("MERCADOR", "MERCHANT") : localization.EnemyName(target), showBlacksmith || dungeonState.IsMerchantFloor ? UiTheme.Gold : target == null ? UiTheme.Dim : target.IsElite ? FloorEventText.TitleColor(target.Titles[0]) : UiTheme.Red, target?.Hurt ?? 0, target?.LastDamage ?? 0, target == null ? "" : StatusText.For(playerState, target, dungeonState, localization));
        asciiCanvas.Text(32, 432, localization.Translate($"VIDA {playerState.Health}/{playerState.MaxHealth}", $"HEALTH {playerState.Health}/{playerState.MaxHealth}"), UiTheme.Red, 15);
        asciiCanvas.Text(32, 453, asciiCanvas.Bar(playerState.Health, playerState.MaxHealth), UiTheme.Red, 16);
        asciiCanvas.Text(32, 481, localization.Translate($"ENERGIA {playerState.Energy}/{playerState.MaxEnergy}", $"ENERGY {playerState.Energy}/{playerState.MaxEnergy}"), UiTheme.Teal, 15);
        asciiCanvas.Text(32, 502, asciiCanvas.Bar(playerState.Energy, playerState.MaxEnergy), UiTheme.Teal, 16);
        asciiCanvas.Text(32, 543, localization.Translate($"NV {playerState.Level}  XP {playerState.Experience}/{heroCombatStats.XpToNext}", $"LV {playerState.Level}  XP {playerState.Experience}/{heroCombatStats.XpToNext}"), UiTheme.Gold, 15);
        asciiCanvas.Text(32, 575, localization.Translate($"POCOES {inventoryState.Potions}  OURO {playerState.Gold}", $"POTIONS {inventoryState.Potions}  GOLD {playerState.Gold}"), UiTheme.Ink, 14);
        asciiCanvas.Text(32, 607, localization.Translate($"TOCHA {(inventoryState.HasLight ? inventoryState.TorchFuel : 0)}", $"TORCH {(inventoryState.HasLight ? inventoryState.TorchFuel : 0)}"), inventoryState.HasLight ? UiTheme.Gold : UiTheme.Dim, 14);
        if (target != null)
        {
            asciiCanvas.Text(1019, 432, target.Health <= 0 ? localization.Translate("DERROTADO", "DEFEATED") : localization.Translate($"VIDA {target.Health}/{target.MaxHealth}", $"HEALTH {target.Health}/{target.MaxHealth}"), UiTheme.Red, 15);
            asciiCanvas.Text(1019, 453, asciiCanvas.Bar(Math.Max(0, target.Health), target.MaxHealth), UiTheme.Red, 16);
        }

        DrawJournalSummary();
        asciiCanvas.Text(272, 121, runState.Screen == "torch_aim" ? localization.Translate("> TOCHA: WASD / SETAS. ESC cancela.", "> TORCH: WASD / ARROWS. ESC cancels.") : runState.IsAiming ? localization.Translate("> MIRA: WASD / SETAS. ESC cancela.", "> AIM: WASD / ARROWS. ESC cancels.") : dungeonState.IsSanctuary(playerState.Position) ? localization.Translate("Ferreiro Perdido [F]: [E] conversar. Runas protegem esta sala.", "Lost Blacksmith [F]: [E] talk. Runes protect this room.") : dungeonState.IsMerchantFloor ? localization.Translate("[E] Converse ao lado de M. [>] Continue sua jornada.", "[E] Talk next to M. [>] Continue your journey.") : dungeonState.Enemies.Any(e => e.IsWarden && e.AbilityWindup > 0 && dungeonState.Visible[e.Position.X, e.Position.Y]) ? localization.Translate("[!] O guardiao prepara um ataque. Afaste-se das marcas!", "[!] The Warden prepares an attack. Leave the marked tiles!") : dungeonState.Modifier != FloorModifier.None ? FloorEventText.Description(dungeonState.Modifier, localization.English) : localization.Translate("[>] Encontre a passagem para as profundezas.", "[>] Find the passage into the depths."), runState.IsAiming ? UiTheme.Gold : UiTheme.Teal, 15);
        var actionFrame = actionEffectsRenderer.GetFrame();
        for (int y = 0; y < GameRules.Height; y++)
            for (int x = 0; x < GameRules.Width; x++)
            {
                if (!dungeonState.Explored[x, y])
                    continue;
                var pos = new Vector2I(x, y);
                var terrain = EnvironmentAppearance.Sample(dungeonState, pos, visualEffects.UiTime);
                char g = terrain.Glyph;
                Color c = terrain.Color;
                if (dungeonState.Visible[x, y])
                {
                    if (dungeonState.Items.TryGetValue(pos, out char item))
                    {
                        g = item;
                        c = item == '!' ? UiTheme.Red : item == '*' ? UiTheme.Teal : UiTheme.Gold;
                    }

                    var e = dungeonState.At(pos);
                    if (e != null)
                    {
                        g = e.IsElite ? char.ToUpperInvariant(e.Glyph) : e.Glyph;
                        c = e.IsElite ? FloorEventText.TitleColor(e.Titles[0]) : e == target ? UiTheme.Gold : UiTheme.Red;
                        if (e.Hurt > 0)
                            g = UiTheme.ImpactGlyph(e.Hurt);
                    }

                    if (dungeonState.IsMerchantFloor && pos == dungeonState.MerchantPosition)
                    {
                        g = 'M';
                        c = UiTheme.Gold;
                    }

                    if (dungeonState.IsSanctuary(pos) && g == '.') { g = ':'; c = UiTheme.Teal; }
                    if (dungeonState.BlacksmithRoom.HasValue && pos == dungeonState.BlacksmithPosition) { g = 'F'; c = UiTheme.Gold; }
                    if (pos == playerState.Position)
                    {
                        g = visualEffects.HeroHurtRemaining > 0 ? UiTheme.ImpactGlyph(visualEffects.HeroHurtRemaining) : '@';
                        c = visualEffects.HeroHurtRemaining > 0 ? UiTheme.Red : UiTheme.Teal;
                    }
                }

                if (pos != playerState.Position && actionFrame.TryGetValue(pos, out var actionGlyph))
                {
                    g = actionGlyph.Character;
                    c = actionGlyph.Color;
                }
                
                if (ExitAppearance.ShowMarker(dungeonState, pos, visualEffects.UiTime, g))
                {
                    g = '>';
                    c = dungeonState.Visible[x, y] ? UiTheme.Gold : UiTheme.Gold.Darkened(.3f);
                }
                asciiCanvas.Text(UiTheme.MapX + x * UiTheme.CellX, UiTheme.MapY + y * UiTheme.CellY, g.ToString(), c, 17);
            }

        foreach (DamageEffect fx in visualEffects.Effects)
        {
            float rise = (float)((UiTheme.HurtDuration - fx.Remaining) * 25);
            asciiCanvas.Text(UiTheme.MapX + fx.Position.X * UiTheme.CellX - 4, UiTheme.MapY + fx.Position.Y * UiTheme.CellY - 16 - rise, $"-{fx.Damage}", fx.Hero ? UiTheme.Red : UiTheme.Gold, 15);
        }

        asciiCanvas.Text(272, 597, localization.Translate("SUAS ACOES / CUSTO / DADOS DE DANO", "YOUR ACTIONS / COST / DAMAGE DICE"), UiTheme.Gold, 13);
        bool advanced = playerState.AdvancedClass != AdvancedClass.None;
        asciiCanvas.Frame(270, 612, 78, advanced ? 5 : 4, UiTheme.Dim, 15, 18);
        asciiCanvas.Text(283, 634, $"[Q] {localization.HeroSkill(playerState)}  |  {heroCombatStats.AbilityCost} EN  |  {heroCombatStats.AbilityDice}", playerState.Energy >= heroCombatStats.AbilityCost ? UiTheme.Teal : UiTheme.Dim, 15);
        if (advanced)
            asciiCanvas.Text(283, 655, $"[R] {AdvancementText.Secondary(playerState.AdvancedClass,localization.English)}  |  {heroCombatStats.SecondaryCost} EN" + (heroCombatStats.Advancement!.NewDiceCount > 0 ? $"  |  {heroCombatStats.SecondaryDice}" : ""), playerState.Energy >= heroCombatStats.SecondaryCost ? UiTheme.Teal : UiTheme.Dim, 15);
        asciiCanvas.Text(283, advanced ? 676 : 655, heroCombatStats.CanShoot ? $"[F] {StatusText.Basic(playerState, localization)}  |  {heroCombatStats.ShotCost} EN  |  {heroCombatStats.ShotDice}" : !heroCombatStats.CanMelee ? localization.Translate("[F] Equipe um arco/cajado compativel.", "[F] Equip a compatible bow/staff.") : localization.Translate($"[Mover contra inimigo] Ataque basico: {heroCombatStats.MeleeDice}", $"[Bump into enemy] Basic attack: {heroCombatStats.MeleeDice}"), UiTheme.Ink, 15);
        asciiCanvas.Rule(advanced ? 698 : 685);
        asciiCanvas.Text(32, advanced ? 720 : 707, localization.Translate((advanced ? "WASD mover | Q/R habilidades | P pocao" : "WASD mover | Q habilidade | P pocao") + " | E interagir | ESPACO esperar | TAB trocar alvo | ESC pausa", (advanced ? "WASD move | Q/R abilities | P potion" : "WASD move | Q ability | P potion") + " | E interact | SPACE wait | TAB switch target | ESC pause"), UiTheme.Ink, 15);
        asciiCanvas.Text(32, 749, localization.Translate("Y tocha fixa   t tocha caida   O barril   o oleo   ^ espinhos Z choque % esporos V fogo   ~ agua = gelo : vapor", "Y fixed torch   t fallen torch   O barrel   o oil   ^ spikes Z shock % spores V flame   ~ water = ice : steam"), UiTheme.Dim, 13);
        asciiCanvas.Text(32, 779, localization.Translate("@ voce   |-+ parede   > escada   ! pocao   $ ouro   * cristal   C bau   M mercador", "@ you   |-+ wall   > stairs   ! potion   $ gold   * crystal   C chest   M merchant"), UiTheme.Dim, 13);
    }

    internal void DrawJournalSummary()
    {
        asciiCanvas.Frame(1012, 478, 25, 12, UiTheme.Dim);
        asciiCanvas.Text(1026, 501, localization.Translate("DIARIO / ROLAGENS", "JOURNAL / ROLLS"), UiTheme.Gold, 12);
        float y = 524;
        if (expeditionJournal.Entries.Count == 0)
            asciiCanvas.Text(1026, y, localization.Translate("Sem registros.", "No entries yet."), UiTheme.Dim, 12);
        for (int i = 0; i < Math.Min(2, expeditionJournal.Entries.Count); i++)
        {
            string remaining = "> " + localization.Translate(expeditionJournal.Entries[i].Pt, expeditionJournal.Entries[i].En);
            for (int line = 0; line < 4 && remaining.Length > 0; line++)
            {
                string text;
                if (remaining.Length <= 28)
                {
                    text = remaining;
                    remaining = "";
                }
                else if (line == 3)
                {
                    text = remaining[..25] + "...";
                    remaining = "";
                }
                else
                {
                    int cut = remaining.LastIndexOf(' ', 27);
                    if (cut < 3)
                        cut = 28;
                    text = remaining[..cut];
                    remaining = "  " + remaining[cut..].TrimStart();
                }

                asciiCanvas.Text(1026, y, text, i == 0 ? UiTheme.Ink : UiTheme.Dim, 12);
                y += 15;
            }

            y += 6;
        }
    }
}
