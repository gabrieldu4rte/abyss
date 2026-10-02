namespace Abyss.Presentation;
internal sealed class AdvancementRenderer(AsciiCanvas canvas, Localization text, PlayerState player, MenuState menu, UiComponents ui)
{
    internal void Draw()
    {
        canvas.Text(32,99,text.Translate("UM NOVO CAMINHO", "A NEW PATH"),UiTheme.Gold,26);
        canvas.Text(32,143,text.Translate("Nivel 10: escolha sua especializacao. A escolha vale por toda a expedicao.", "Level 10: choose your specialization. This choice lasts for the expedition."),UiTheme.Ink,17);
        var choices = AdvancementCatalog.Options(player.ClassIndex);
        for (int i = 0; i < 2; i++)
        {
            var choice = choices[i];
            float x = 32 + i * 624;
            var tint = i == menu.AdvancementIndex ? UiTheme.Teal : UiTheme.Dim;
            canvas.Text(x,194,(i == menu.AdvancementIndex ? "> " : "  ") + $"[{i+1}] " + AdvancementText.Name(choice,text.English).ToUpperInvariant(),tint,22);
            canvas.Portrait(x,225,AsciiArt.AdvancedHero(choice,false),AdvancementText.Name(choice,text.English).ToUpperInvariant(),tint);
            canvas.WrappedText(x+245,256,AdvancementText.Description(choice,text.English),UiTheme.Ink,16,35,26);
            canvas.WrappedText(x+245,458,"[Q] " + AdvancementText.Primary(choice,text.English),UiTheme.Teal,16,34,25);
            canvas.WrappedText(x+245,523,"[R] " + AdvancementText.Secondary(choice,text.English),UiTheme.Gold,16,34,25);
        }
        if (menu.AdvancementConfirm)
        {
            canvas.Text(32,640,text.Translate("Confirmar: ", "Confirm: ") + AdvancementText.Name(choices[menu.AdvancementIndex],text.English) + "?",UiTheme.Gold,21);
            canvas.Text(650,640,(menu.ConfirmYes ? "  " : "> ") + text.Translate("NAO", "NO") + "     " + (menu.ConfirmYes ? "> " : "  ") + text.Translate("SIM", "YES"),UiTheme.Teal,21);
            ui.Footer("[SETAS / A D] escolher   [ENTER] confirmar   [ESC] cancelar", "[ARROWS / A D] choose   [ENTER] confirm   [ESC] cancel");
        }
        else
        {
            canvas.Text(32,640,text.Translate("Pode decidir depois pela ficha do personagem: [C].", "You can decide later from the character sheet: [C]."),UiTheme.Dim,17);
            ui.Footer("[1-2 / A D] especializacao   [ENTER] escolher   [ESC] decidir depois", "[1-2 / A D] specialization   [ENTER] choose   [ESC] decide later");
        }
    }
}
