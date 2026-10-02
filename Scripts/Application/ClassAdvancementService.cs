using Godot;
using System;
namespace Abyss.Application;
internal sealed class ClassAdvancementService(PlayerState player, MenuState menu, RunState run, ExpeditionJournal journal, VisualEffects effects)
{
    internal bool Available => player.Level >= 10 && player.AdvancedClass == AdvancedClass.None && player.Health > 0;
    internal void Offer()
    {
        if (Available && !menu.AdvancementDeferred && run.Screen == "game" && !effects.Actions.Active) Open("game");
    }
    internal void Open(string returnScreen)
    {
        if (!Available) return;
        menu.AdvancementReturn = returnScreen;
        menu.AdvancementIndex = 0;
        menu.AdvancementConfirm = false;
        menu.ConfirmYes = false;
        run.IsAiming = false;
        run.Screen = "advancement";
    }
    internal bool Choose(AdvancedClass choice)
    {
        if (!Available || !Array.Exists(AdvancementCatalog.Options(player.ClassIndex), c => c == choice)) return false;
        player.AdvancedClass = choice;
        menu.AdvancementDeferred = false;
        menu.AdvancementConfirm = false;
        effects.Sounds.Play("levelup");
        journal.Say($"Voce avancou para {AdvancementText.Name(choice, false)}. [R] nova habilidade.", $"You advanced to {AdvancementText.Name(choice, true)}. [R] new ability.");
        run.Screen = menu.AdvancementReturn;
        return true;
    }
    internal void Handle(Key key)
    {
        if (menu.AdvancementConfirm)
        {
            if (key == Key.Escape) { menu.AdvancementConfirm = false; return; }
            if (UiTheme.Previous(key) || UiTheme.Next(key) || key is Key.Left or Key.Right or Key.A or Key.D) menu.ConfirmYes = !menu.ConfirmYes;
            if (UiTheme.Confirm(key))
            {
                if (menu.ConfirmYes) Choose(AdvancementCatalog.Options(player.ClassIndex)[menu.AdvancementIndex]);
                else menu.AdvancementConfirm = false;
            }
            return;
        }
        if (key == Key.Escape) { menu.AdvancementDeferred = true; run.Screen = menu.AdvancementReturn; return; }
        if (key is Key.Left or Key.Right or Key.A or Key.D || UiTheme.Previous(key) || UiTheme.Next(key)) menu.AdvancementIndex = 1 - menu.AdvancementIndex;
        if (key == Key.Key1) menu.AdvancementIndex = 0;
        if (key == Key.Key2) menu.AdvancementIndex = 1;
        if (UiTheme.Confirm(key)) { menu.AdvancementConfirm = true; menu.ConfirmYes = false; }
    }
}
