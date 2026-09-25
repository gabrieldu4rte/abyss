using Godot;
using System;

namespace Abyss.Application;
internal sealed class GameInput
{
    private readonly ITurnScheduler turns;
    private readonly ExpeditionJournal expeditionJournal;
    private readonly IGameHost host;
    private readonly MenuController menuController;
    private readonly MenuState menuState;
    private readonly PlayerActions playerActions;
    private readonly PlayerState playerState;
    private readonly RunState runState;
    private readonly VisualEffects visualEffects;
    internal GameInput(ITurnScheduler turns, ExpeditionJournal expeditionJournal, IGameHost host, MenuController menuController, MenuState menuState, PlayerActions playerActions, PlayerState playerState, RunState runState, VisualEffects visualEffects)
    {
        this.turns = turns;
        this.expeditionJournal = expeditionJournal;
        this.host = host;
        this.menuController = menuController;
        this.menuState = menuState;
        this.playerActions = playerActions;
        this.playerState = playerState;
        this.runState = runState;
        this.visualEffects = visualEffects;
    }

    public void HandleEvent(InputEvent e)
    {
        if (e is not InputEventKey k || k.Echo)
            return;
        if (!k.Pressed)
        {
            if (k.Keycode == HeldMovementKey)
                StopHeldMovement();
            return;
        }

        StopHeldMovement();
        if (runState.Screen == "game" && !runState.IsAiming && UiTheme.MovementDirection(k.Keycode) != Vector2I.Zero)
        {
            HeldMovementKey = k.Keycode;
            MovementDelay = .30;
        }

        HandleKey(k.Keycode);
        host.RequestRedraw();
    }

    internal void HandleKey(Key key)
    {
        if (key == Key.F11)
        {
            host.ToggleFullscreen();
            return;
        }

        if (menuController.HandleMenus(key))
            return;
        if (key == Key.I)
        {
            runState.Screen = "pause";
            menuState.PauseTab = 1;
            menuState.InventoryIndex = 0;
            menuState.InventoryNotice = ("", "");
            return;
        }

        if (key == Key.Escape)
        {
            if (runState.IsAiming)
                runState.IsAiming = false;
            else
            {
                runState.Screen = "pause";
                menuState.MenuIndex = 0;
                menuState.PauseTab = 0;
                menuState.JournalPage = 0;
            }

            return;
        }

        if (key == Key.Tab)
        {
            visualEffects.CycleTarget();
            return;
        }

        Vector2I d = UiTheme.MovementDirection(key);
        if (d != Vector2I.Zero)
        {
            if (runState.IsAiming)
            {
                playerActions.Shoot(d);
                runState.IsAiming = false;
            }
            else
                playerActions.Move(d);
        }
        else if (key == Key.F)
            playerActions.BeginAim();
        else if (key == Key.Q)
            playerActions.Skill();
        else if (key == Key.P)
            playerActions.Drink();
        else if (key == Key.Space)
        {
            expeditionJournal.Say("Voce espera e recupera 1 de energia.", "You wait and recover 1 energy.");
            playerState.Energy = Math.Min(playerState.MaxEnergy, playerState.Energy + 1);
            turns.EndTurn();
        }
        else if (key == Key.E)
            playerActions.Interact();
    }

    internal Key HeldMovementKey = Key.None;
    internal double MovementDelay;
    internal void StopHeldMovement()
    {
        HeldMovementKey = Key.None;
        MovementDelay = 0;
    }

    public void OnNotification(int what)
    {
        if (what == Node.NotificationApplicationFocusOut)
            StopHeldMovement();
    }

    internal void AdvanceHeldMovement(double delta)
    {
        if (runState.Screen != "game" || runState.IsAiming)
        {
            StopHeldMovement();
            return;
        }

        if (HeldMovementKey == Key.None)
            return;
        MovementDelay -= delta;
        if (MovementDelay > 0)
            return;
        // At most one turn per frame, even after a stall.
        MovementDelay = .16;
        playerActions.Move(UiTheme.MovementDirection(HeldMovementKey));
        host.RequestRedraw();
    }
}
