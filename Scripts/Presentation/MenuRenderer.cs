using Godot;

namespace Abyss.Presentation;
internal sealed class MenuRenderer
{
    private readonly AsciiCanvas asciiCanvas;
    private readonly DungeonState dungeonState;
    private readonly Localization localization;
    private readonly MenuState menuState;
    private readonly PlayerState playerState;
    private readonly RunState runState;
    private readonly UiComponents uiComponents;
    internal MenuRenderer(AsciiCanvas asciiCanvas, DungeonState dungeonState, Localization localization, MenuState menuState, PlayerState playerState, RunState runState, UiComponents uiComponents)
    {
        this.asciiCanvas = asciiCanvas;
        this.dungeonState = dungeonState;
        this.localization = localization;
        this.menuState = menuState;
        this.playerState = playerState;
        this.runState = runState;
        this.uiComponents = uiComponents;
    }

    internal void DrawHome()
    {
        asciiCanvas.Text(32, 99, localization.Translate("A PORTA DO ABISMO", "THE GATE TO THE ABYSS"), UiTheme.Teal, 16);
        asciiCanvas.Art(32, 169, AsciiArt.Tower, UiTheme.Dim);
        asciiCanvas.Text(655, 210, localization.Translate("A luz termina aqui.", "The light ends here."), UiTheme.Gold, 30);
        uiComponents.MenuItem(655, 414, 0, localization.Translate("INICIAR JOGO", "START GAME"));
        uiComponents.MenuItem(655, 473, 1, localization.Translate("CONFIGURACOES", "SETTINGS"));
        uiComponents.MenuItem(655, 532, 2, localization.Translate("SAIR DO JOGO", "QUIT GAME"));
        uiComponents.Footer("[SETAS / W S] selecionar   [ENTER] confirmar   [F11] tela cheia", "[ARROWS / W S] select   [ENTER] confirm   [F11] fullscreen");
    }

    internal void DrawLanguage()
    {
        asciiCanvas.Text(32, 99, localization.Translate("02 / IDIOMA", "02 / LANGUAGE"), UiTheme.Teal, 16);
        asciiCanvas.Art(40, 181, AsciiArt.Globe, UiTheme.Dim);
        asciiCanvas.Text(650, 210, "PORTUGUES / ENGLISH", UiTheme.Gold, 26);
        asciiCanvas.Lines(650, 269, localization.Translate("Escolha o idioma da sua aventura.\nA preferencia sera salva neste computador.", "Choose the language of your adventure.\nYour preference is saved on this computer."), UiTheme.Ink, 17, 29);
        asciiCanvas.Text(650, 401, (menuState.LanguageIndex == 0 ? "> " : "  ") + "[1] Portugues (Brasil)", menuState.LanguageIndex == 0 ? UiTheme.Teal : UiTheme.Dim, 22);
        asciiCanvas.Text(650, 461, (menuState.LanguageIndex == 1 ? "> " : "  ") + "[2] English", menuState.LanguageIndex == 1 ? UiTheme.Teal : UiTheme.Dim, 22);
        uiComponents.Footer("[SETAS / 1-2] selecionar   [ENTER] aplicar   [ESC] voltar", "[ARROWS / 1-2] select   [ENTER] apply   [ESC] back");
    }

    internal void DrawClasses()
    {
        asciiCanvas.Text(32, 99, localization.Translate("ESCOLHA QUEM VAI DESCER", "CHOOSE WHO WILL DESCEND"), UiTheme.Teal, 16);
        asciiCanvas.Text(32, 147, localization.Translate("Quatro destinos. Uma unica saida.", "Four destinies. One way out."), UiTheme.Gold, 27);
        string[] pt =
        {
            "Resiste e luta de perto.",
            "Controla grupos com magia.",
            "Acerta de longa distancia.",
            "Golpe forte e evasao."
        };
        string[] en =
        {
            "Armored close combat.",
            "Controls groups with magic.",
            "Strikes from a distance.",
            "Heavy strikes and evasion."
        };
        for (int i = 0; i < 4; i++)
        {
            float x = 32 + i * 309;
            Color color = i == playerState.ClassIndex ? UiTheme.Teal : UiTheme.Dim;
            asciiCanvas.Portrait(x + 26, 195, AsciiArt.Heroes[i], $"[{i + 1}] {localization.ClassName(i)}", color);
            float portraitWidth = asciiCanvas.Font.GetStringSize(new string ('-', 25), HorizontalAlignment.Left, -1, 15).X;
            string description = localization.Translate(pt[i], en[i]);
            float descriptionWidth = asciiCanvas.Font.GetStringSize(description, HorizontalAlignment.Left, -1, 15).X;
            asciiCanvas.Text(x + 26 + (portraitWidth - descriptionWidth) / 2, 528, description, UiTheme.Ink, 15);
            string selection = i == playerState.ClassIndex ? localization.Translate("> HEROI SELECIONADO <", "> HERO SELECTED <") : ". . .";
            float selectionWidth = asciiCanvas.Font.GetStringSize(selection, HorizontalAlignment.Left, -1, 15).X;
            asciiCanvas.Text(x + 26 + (portraitWidth - selectionWidth) / 2, 576, selection, color, 15);
        }

        uiComponents.Footer("[1-4 / A D] classe   [ENTER] iniciar expedicao   [ESC] voltar", "[1-4 / A D] class   [ENTER] start expedition   [ESC] back");
    }

    internal void DrawEnd()
    {
        asciiCanvas.Text(32, 99, localization.Translate("O ABISMO GUARDA SUA HISTORIA", "THE ABYSS KEEPS YOUR STORY"), UiTheme.Teal, 16);
        asciiCanvas.Art(32, 205, AsciiArt.Grave, UiTheme.Dim);
        asciiCanvas.Text(548, 195, localization.Translate("SUA EXPEDICAO TERMINOU", "YOUR EXPEDITION HAS ENDED"), UiTheme.Red, 25);
        asciiCanvas.Text(548, 235, localization.Translate($"ANDAR ALCANCADO: {dungeonState.Floor}", $"FLOOR REACHED: {dungeonState.Floor}"), UiTheme.Gold, 22);
        asciiCanvas.Text(548, 266, $"{localization.ClassName(playerState.ClassIndex)} / {localization.Translate("NIVEL", "LEVEL")} {playerState.Level}", UiTheme.Teal, 17);
        asciiCanvas.Portrait(548, 285, HeroPortrait.Select(playerState), localization.ClassName(playerState.ClassIndex), UiTheme.Teal);
        asciiCanvas.Lines(827, 352, localization.Translate($"{playerState.Kills} inimigos derrotados\n{playerState.Gold} moedas coletadas\n{runState.Turn} turnos", $"{playerState.Kills} enemies defeated\n{playerState.Gold} gold collected\n{runState.Turn} turns"), UiTheme.Ink, 17, 31);
        uiComponents.Footer("[ENTER] voltar ao menu inicial", "[ENTER] return to the main menu");
    }

    internal void DrawQuitConfirm()
    {
        asciiCanvas.DrawAsciiImage(32, 139, AsciiArt.Camp, 510, 510, new Color("fff6df"), 2);
        asciiCanvas.Text(600, 223, localization.Translate("SAIR DO JOGO?", "QUIT THE GAME?"), UiTheme.Gold, 23);
        asciiCanvas.Text(600, 290, localization.Translate("Tem certeza de que deseja sair?", "Are you sure you want to quit?"), UiTheme.Ink, 20);
        asciiCanvas.Text(600, 410, (menuState.ExitYes ? "  " : "> ") + localization.Translate("NAO, FICAR", "NO, STAY"), menuState.ExitYes ? UiTheme.Dim : UiTheme.Teal, 22);
        asciiCanvas.Text(600, 474, (menuState.ExitYes ? "> " : "  ") + localization.Translate("SIM, SAIR DO JOGO", "YES, QUIT GAME"), menuState.ExitYes ? UiTheme.Teal : UiTheme.Dim, 22);
        uiComponents.Footer("[SETAS] escolher   [ENTER] confirmar   [ESC] cancelar", "[ARROWS] choose   [ENTER] confirm   [ESC] cancel");
    }

    internal void DrawExitConfirm()
    {
        asciiCanvas.DrawAsciiImage(32, 139, AsciiArt.Camp, 510, 510, new Color("fff6df"), 2);
        asciiCanvas.Text(600, 223, localization.Translate("VOLTAR AO MENU PRINCIPAL?", "RETURN TO THE MAIN MENU?"), UiTheme.Gold, 23);
        asciiCanvas.Lines(600, 290, localization.Translate("A expedicao sera encerrada.\nSeu progresso nao sera salvo.", "The expedition will end.\nYour progress will not be saved."), UiTheme.Ink, 20, 35);
        asciiCanvas.Text(600, 410, (menuState.ExitYes ? "  " : "> ") + localization.Translate("NAO, CONTINUAR", "NO, CONTINUE"), menuState.ExitYes ? UiTheme.Dim : UiTheme.Teal, 22);
        asciiCanvas.Text(600, 474, (menuState.ExitYes ? "> " : "  ") + localization.Translate("SIM, VOLTAR AO MENU", "YES, RETURN TO MENU"), menuState.ExitYes ? UiTheme.Teal : UiTheme.Dim, 22);
        uiComponents.Footer("[SETAS] escolher   [ENTER] confirmar   [ESC] cancelar", "[ARROWS] choose   [ENTER] confirm   [ESC] cancel");
    }
}
