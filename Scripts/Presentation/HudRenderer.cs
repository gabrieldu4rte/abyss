using Godot;
using System;

namespace Abyss.Presentation;
internal sealed class HudRenderer
{
    private readonly AsciiCanvas asciiCanvas;
    private readonly DungeonState dungeonState;
    private readonly ExpeditionJournal expeditionJournal;
    private readonly HeroCombatStats heroCombatStats;
    private readonly InventoryState inventoryState;
    private readonly Localization localization;
    private readonly PlayerState playerState;
    private readonly RunState runState;
    private readonly VisualEffects visualEffects;
    internal HudRenderer(AsciiCanvas asciiCanvas, DungeonState dungeonState, ExpeditionJournal expeditionJournal, HeroCombatStats heroCombatStats, InventoryState inventoryState, Localization localization, PlayerState playerState, RunState runState, VisualEffects visualEffects)
    {
        this.asciiCanvas = asciiCanvas;
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
        asciiCanvas.Text(1012, 91, dungeonState.IsMerchantFloor ? localization.Translate("MERCADOR", "MERCHANT") : localization.Translate("ALVO SELECIONADO", "SELECTED TARGET"), dungeonState.IsMerchantFloor ? UiTheme.Gold : UiTheme.Red, 16);
        var target = visualEffects.FocusEnemy();
        asciiCanvas.Portrait(24, 121, AsciiArt.Heroes[playerState.ClassIndex], "@ " + localization.ClassName(playerState.ClassIndex), UiTheme.Teal, visualEffects.HeroHurtRemaining, visualEffects.HeroDamage);
        asciiCanvas.Portrait(1012, 121, dungeonState.IsMerchantFloor ? AsciiArt.Merchant : AsciiArt.Enemy(target?.Glyph ?? '?'), dungeonState.IsMerchantFloor ? localization.Translate("MERCADOR", "MERCHANT") : localization.EnemyName(target?.Glyph ?? '?'), dungeonState.IsMerchantFloor ? UiTheme.Gold : target == null ? UiTheme.Dim : UiTheme.Red, target?.Hurt ?? 0, target?.LastDamage ?? 0);
        asciiCanvas.Text(32, 432, localization.Translate($"VIDA {playerState.Health}/{playerState.MaxHealth}", $"HEALTH {playerState.Health}/{playerState.MaxHealth}"), UiTheme.Red, 15);
        asciiCanvas.Text(32, 453, asciiCanvas.Bar(playerState.Health, playerState.MaxHealth), UiTheme.Red, 16);
        asciiCanvas.Text(32, 481, localization.Translate($"ENERGIA {playerState.Energy}/{playerState.MaxEnergy}", $"ENERGY {playerState.Energy}/{playerState.MaxEnergy}"), UiTheme.Teal, 15);
        asciiCanvas.Text(32, 502, asciiCanvas.Bar(playerState.Energy, playerState.MaxEnergy), UiTheme.Teal, 16);
        asciiCanvas.Text(32, 543, localization.Translate($"NV {playerState.Level}  XP {playerState.Experience}/{heroCombatStats.XpToNext}", $"LV {playerState.Level}  XP {playerState.Experience}/{heroCombatStats.XpToNext}"), UiTheme.Gold, 15);
        asciiCanvas.Text(32, 575, localization.Translate($"POCOES {inventoryState.Potions}  OURO {playerState.Gold}", $"POTIONS {inventoryState.Potions}  GOLD {playerState.Gold}"), UiTheme.Ink, 14);
        if (target != null)
        {
            asciiCanvas.Text(1019, 432, target.Health <= 0 ? localization.Translate("DERROTADO", "DEFEATED") : localization.Translate($"VIDA {target.Health}/{target.MaxHealth}", $"HEALTH {target.Health}/{target.MaxHealth}"), UiTheme.Red, 15);
            asciiCanvas.Text(1019, 453, asciiCanvas.Bar(Math.Max(0, target.Health), target.MaxHealth), UiTheme.Red, 16);
        }

        DrawJournalSummary();
        asciiCanvas.Text(272, 121, runState.IsAiming ? localization.Translate("> MIRA: WASD / SETAS. ESC cancela.", "> AIM: WASD / ARROWS. ESC cancels.") : dungeonState.IsMerchantFloor ? localization.Translate("[E] Converse ao lado de M. [>] Continue sua jornada.", "[E] Talk next to M. [>] Continue your journey.") : localization.Translate("[>] Encontre a passagem para as profundezas.", "[>] Find the passage into the depths."), runState.IsAiming ? UiTheme.Gold : UiTheme.Teal, 15);
        for (int y = 0; y < GameRules.Height; y++)
            for (int x = 0; x < GameRules.Width; x++)
            {
                if (!dungeonState.Explored[x, y])
                    continue;
                char g = dungeonState.Tiles[x, y];
                Color c = dungeonState.Visible[x, y] ? (g == '#' ? new Color("71879a") : new Color("354452")) : new Color("23303e");
                var pos = new Vector2I(x, y);
                if (g == '>')
                    c = dungeonState.Visible[x, y] ? UiTheme.Gold : UiTheme.Dim;
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
                        g = e.Glyph;
                        c = e == target ? UiTheme.Gold : UiTheme.Red;
                        if (e.Hurt > 0)
                            g = UiTheme.ImpactGlyph(e.Hurt);
                    }

                    if (dungeonState.IsMerchantFloor && pos == dungeonState.MerchantPosition)
                    {
                        g = 'M';
                        c = UiTheme.Gold;
                    }

                    if (pos == playerState.Position)
                    {
                        g = visualEffects.HeroHurtRemaining > 0 ? UiTheme.ImpactGlyph(visualEffects.HeroHurtRemaining) : '@';
                        c = visualEffects.HeroHurtRemaining > 0 ? UiTheme.Red : UiTheme.Teal;
                    }
                }

                asciiCanvas.Text(UiTheme.MapX + x * UiTheme.CellX, UiTheme.MapY + y * UiTheme.CellY, g.ToString(), c, 17);
            }

        foreach (DamageEffect fx in visualEffects.Effects)
        {
            float rise = (float)((UiTheme.HurtDuration - fx.Remaining) * 25);
            asciiCanvas.Text(UiTheme.MapX + fx.Position.X * UiTheme.CellX - 4, UiTheme.MapY + fx.Position.Y * UiTheme.CellY - 16 - rise, $"-{fx.Damage}", fx.Hero ? UiTheme.Red : UiTheme.Gold, 15);
        }

        asciiCanvas.Text(272, 597, localization.Translate("SUAS ACOES / CUSTO / DADOS DE DANO", "YOUR ACTIONS / COST / DAMAGE DICE"), UiTheme.Gold, 13);
        asciiCanvas.Frame(270, 612, 78, 4, UiTheme.Dim, 15, 18);
        asciiCanvas.Text(283, 634, $"[Q] {localization.SkillName(playerState.ClassIndex)}  |  {heroCombatStats.AbilityCost} EN  |  {heroCombatStats.AbilityDice}", playerState.Energy >= heroCombatStats.AbilityCost ? UiTheme.Teal : UiTheme.Dim, 15);
        asciiCanvas.Text(283, 655, heroCombatStats.CanShoot ? $"[F] {localization.Translate("Basico", "Basic")}  |  {heroCombatStats.ShotCost} EN  |  {heroCombatStats.ShotDice}" : !heroCombatStats.CanMelee ? localization.Translate("[F] Equipe um arco/cajado compativel.", "[F] Equip a compatible bow/staff.") : localization.Translate($"[Mover contra inimigo] Ataque basico: {heroCombatStats.MeleeDice}", $"[Bump into enemy] Basic attack: {heroCombatStats.MeleeDice}"), UiTheme.Ink, 15);
        asciiCanvas.Rule(685);
        asciiCanvas.Text(32, 707, localization.Translate("WASD mover | Q habilidade | P pocao | E interagir | ESPACO esperar | TAB trocar alvo | ESC pausa", "WASD move | Q ability | P potion | E interact | SPACE wait | TAB switch target | ESC pause"), UiTheme.Ink, 15);
        asciiCanvas.Text(32, 779, localization.Translate("@ voce   # parede   > escada   ! pocao   $ ouro   * cristal   C bau   M mercador", "@ you   # wall   > stairs   ! potion   $ gold   * crystal   C chest   M merchant"), UiTheme.Dim, 13);
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
