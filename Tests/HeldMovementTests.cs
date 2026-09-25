using Godot;
using System;

namespace Abyss.Tests;
internal sealed partial class RegressionSuite
{
    internal void TestHeldMovement()
    {
        game.PlayerState.ClassIndex = 0;
        game.Start(123);
        game.DungeonState.Enemies.Clear();
        game.DungeonState.Items.Clear();
        game.DungeonState.IsMerchantFloor = false;
        for (int x = 1; x < GameRules.Width - 1; x++)
            for (int y = 1; y < GameRules.Height - 1; y++)
                game.DungeonState.Tiles[x, y] = '.';
        game.PlayerState.Position = new Vector2I(10, 10);
        void Press(Key key, bool pressed = true, bool echo = false) => game.GameInput.HandleEvent(new InputEventKey { Keycode = key, Pressed = pressed, Echo = echo });
        Press(Key.D);
        if (game.PlayerState.Position != new Vector2I(11, 10) || game.RunState.Turn != 1)
            throw new Exception("Initial movement failed");
        Press(Key.D, echo: true);
        game.GameInput.AdvanceHeldMovement(.20);
        if (game.RunState.Turn != 1)
            throw new Exception("Movement repeated too soon");
        game.GameInput.AdvanceHeldMovement(.11);
        game.GameInput.AdvanceHeldMovement(.17);
        if (game.PlayerState.Position != new Vector2I(13, 10) || game.RunState.Turn != 3)
            throw new Exception("Held movement failed");
        Press(Key.D, false);
        game.GameInput.AdvanceHeldMovement(1);
        if (game.RunState.Turn != 3)
            throw new Exception("Movement continued after release");
        Press(Key.Right);
        Press(Key.Escape);
        game.GameInput.AdvanceHeldMovement(1);
        Press(Key.Escape);
        game.GameInput.AdvanceHeldMovement(1);
        if (game.RunState.Turn != 4)
            throw new Exception("Movement leaked through pause");
        Press(Key.D);
        game.GameInput.OnNotification((int)Node.NotificationApplicationFocusOut);
        game.GameInput.AdvanceHeldMovement(1);
        if (game.RunState.Turn != 5)
            throw new Exception("Movement continued without focus");
        game.RunState.IsAiming = true;
        Press(Key.Left);
        game.GameInput.AdvanceHeldMovement(1);
        if (game.GameInput.HeldMovementKey != Key.None)
            throw new Exception("Aiming started held movement");
        game.GameInput.StopHeldMovement();
        GD.Print("MOVEMENT AUDIT: immediate step, delay, repeat, release, pause, focus and aiming passed.");
    }
}
