using Godot;
using System;

public partial class Main
{
    Key heldMovementKey=Key.None;
    double movementDelay;
    static Vector2I MovementDirection(Key key)=>key switch {
        Key.W or Key.Up=>Vector2I.Up,Key.S or Key.Down=>Vector2I.Down,
        Key.A or Key.Left=>Vector2I.Left,Key.D or Key.Right=>Vector2I.Right,_=>Vector2I.Zero};
    void StopHeldMovement(){heldMovementKey=Key.None;movementDelay=0;}
    public override void _Notification(int what)
    {
        if(what==NotificationApplicationFocusOut)StopHeldMovement();
    }
    void AdvanceHeldMovement(double delta)
    {
        if(screen!="game"||aiming){StopHeldMovement();return;}
        if(heldMovementKey==Key.None)return;
        movementDelay-=delta;
        if(movementDelay>0)return;
        // At most one turn per frame, even after a stall.
        movementDelay=.16;
        Move(MovementDirection(heldMovementKey));
        QueueRedraw();
    }
    void TestHeldMovement()
    {
        selected=0;Start(123);enemies.Clear();items.Clear();merchantFloor=false;
        for(int x=1;x<W-1;x++)for(int y=1;y<H-1;y++)map[x,y]='.';
        player=new Vector2I(10,10);
        void Press(Key key,bool pressed=true,bool echo=false)=>_UnhandledKeyInput(new InputEventKey{Keycode=key,Pressed=pressed,Echo=echo});
        Press(Key.D);
        if(player!=new Vector2I(11,10)||turn!=1)throw new Exception("Initial movement failed");
        Press(Key.D,echo:true);AdvanceHeldMovement(.20);
        if(turn!=1)throw new Exception("Movement repeated too soon");
        AdvanceHeldMovement(.11);AdvanceHeldMovement(.17);
        if(player!=new Vector2I(13,10)||turn!=3)throw new Exception("Held movement failed");
        Press(Key.D,false);AdvanceHeldMovement(1);
        if(turn!=3)throw new Exception("Movement continued after release");
        Press(Key.Right);Press(Key.Escape);AdvanceHeldMovement(1);Press(Key.Escape);AdvanceHeldMovement(1);
        if(turn!=4)throw new Exception("Movement leaked through pause");
        Press(Key.D);_Notification((int)NotificationApplicationFocusOut);AdvanceHeldMovement(1);
        if(turn!=5)throw new Exception("Movement continued without focus");
        aiming=true;Press(Key.Left);AdvanceHeldMovement(1);
        if(heldMovementKey!=Key.None)throw new Exception("Aiming started held movement");
        StopHeldMovement();
        GD.Print("MOVEMENT AUDIT: immediate step, delay, repeat, release, pause, focus and aiming passed.");
    }
}
