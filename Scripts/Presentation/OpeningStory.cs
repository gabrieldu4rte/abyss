using Godot;
using System;

namespace Abyss.Presentation;
internal sealed class OpeningStory(RunState runState, Localization localization)
{
    private double elapsed;
    private const double CharactersPerSecond = 42;
    internal string Story => localization.Translate(
        "> Ha muito, a luz abandonou estas ruinas.\n\n> Sob a pedra, o Abismo ainda desperta.\n> Muitos desceram em busca de ouro e respostas.\n> Apenas seus ecos voltaram.\n\n> Agora, seus passos chegam a entrada.\n> Ate onde voce ousara descer?",
        "> Long ago, light abandoned these ruins.\n\n> Beneath the stone, the Abyss still stirs.\n> Many descended seeking gold and answers.\n> Only their echoes returned.\n\n> Now, your footsteps reach the entrance.\n> How far will you dare to descend?");
    internal int VisibleCharacters => Math.Min(Story.Length, (int)(elapsed * CharactersPerSecond + .000001));
    internal bool Complete => VisibleCharacters == Story.Length;
    internal bool CursorVisible => (int)(elapsed * 2) % 2 == 0;
    internal void Begin() { elapsed = 0; runState.Screen = "intro"; }
    internal void Advance(double delta)
    {
        if (runState.Screen != "intro") return;
        elapsed += delta;
        if (elapsed >= Story.Length / CharactersPerSecond + 2)
            runState.Screen = "home";
    }
    internal void HandleKey(Key key)
    {
        if (key == Key.Escape) runState.Screen = "home";
        else if (UiTheme.Confirm(key))
        {
            if (Complete) runState.Screen = "home";
            else elapsed = Story.Length / CharactersPerSecond;
        }
    }
    internal void Draw(AsciiCanvas canvas)
    {
        canvas.Text(110, 175, localization.Translate("ECOS SOB A PEDRA", "ECHOES BENEATH THE STONE"), UiTheme.Gold, 22);
        canvas.Text(110, 214, new string('-', 66), UiTheme.Dim, 18);
        canvas.Lines(110, 275, Story[..VisibleCharacters] + (CursorVisible ? "_" : ""), UiTheme.Teal, 21, 37);
        canvas.Text(110, 700, localization.Translate("[ENTER] continuar   [ESC] pular", "[ENTER] continue   [ESC] skip"), UiTheme.Dim, 16);
    }
}
