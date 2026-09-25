using System;

public partial class Main
{
    static float AsciiGlow(int mode,float x,float y,double time)
    {
        double cx=mode==3?.75:.5,cy=mode==2?.68:mode==3?.24:.45;
        double radius=mode==1?2:9;
        double falloff=Math.Exp(-radius*((x-cx)*(x-cx)+(y-cy)*(y-cy)));
        double pulse=mode==1?.10*Math.Sin(time*.85):.10*Math.Sin(time*2.1)+.025*Math.Sin(time*5.3);
        return (float)(.88+falloff*(.16+pulse));
    }
}
