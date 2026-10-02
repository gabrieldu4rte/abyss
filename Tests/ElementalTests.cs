using Godot;
using System;
using System.Linq;
namespace Abyss.Tests;
internal sealed partial class RegressionSuite
{
    internal void TestElements()
    {
        void Check(bool value, string message) { if (!value) throw new Exception(message); }
        var g = new GameSession(new TestHost(),new TestCanvas(),new TestSettings());
        g.PlayerState.ClassIndex = 1; g.Start(731);
        var d = g.DungeonState; var w = d.Environment;
        d.Enemies.Clear(); d.Items.Clear(); w.Clear(); d.IsMerchantFloor = false; d.BlacksmithRoom = null;
        for (int x = 1; x < GameRules.Width - 1; x++) for (int y = 1; y < GameRules.Height - 1; y++) { d.Tiles[x,y]='.'; d.Visible[x,y]=true; }
        g.PlayerState.Position = new Vector2I(10,10); d.Stairs = new Vector2I(30,20);
        var p = new Vector2I(12,10);
        g.PlayerState.AdvancedClass = AdvancedClass.Cryomancer;
        w.Details[p] = '~'; g.EnvironmentService.ReactElement(p);
        Check(w.Ice.ContainsKey(p) && !g.EnvironmentService.Water(p), "Ice did not freeze water.");
        var e = new Enemy(p, 'r', 1) { Health = 100, MaxHealth = 100 };
        d.Enemies.Add(e); g.EnvironmentService.ElementalHit(e);
        Check(e.FrozenTurns == 2, "Wet target did not receive stronger freeze.");
        g.PlayerState.AdvancedClass = AdvancedClass.Pyromancer;
        g.EnvironmentService.ElementalHit(e);
        Check(e.Health == 98 && e.FrozenTurns == 0 && !w.Ice.ContainsKey(p) && g.EnvironmentService.Water(p), "Thermal shock or melting failed.");
        g.EnvironmentService.ReactElement(p);
        Check(w.Steam.ContainsKey(p) && e.BlindTurns == 2 && !w.Fire.ContainsKey(p), "Water did not generate blinding steam.");
        var oil = new Vector2I(14,10); w.Oil.Add(oil); g.EnvironmentService.ReactElement(oil);
        Check(w.Fire.ContainsKey(oil) && !w.Oil.Contains(oil), "Fire did not ignite oil.");
        g.PlayerState.AdvancedClass = AdvancedClass.Cryomancer; g.EnvironmentService.ReactElement(oil);
        Check(!w.Fire.ContainsKey(oil) && w.Ice.ContainsKey(oil), "Ice did not extinguish fire.");
        var trap = new Vector2I(16,10); w.Details[trap]='~'; w.Fixtures[trap]=Fixture.PoisonTrap;
        g.EnvironmentService.ReactElement(trap); g.PlayerState.Position=trap;
        g.EnvironmentService.Tick();
        Check(w.Fixtures[trap] == Fixture.PoisonTrap && w.HeroPoisonTurns == 0, "Frozen trap was not suppressed.");
        g.PlayerState.Position = new Vector2I(10,10);
        for(int i=0;i<4;i++) g.EnvironmentService.Tick();
        Check(w.Ice.Count == 0 && w.Steam.Count == 0 && w.Details[p]=='~', "Temporary reactions did not expire or damaged permanent water.");
        foreach(var choice in new[]{AdvancedClass.Pyromancer,AdvancedClass.Cryomancer})
        {
            g.PlayerState.AdvancedClass=choice; w.Oil.Add(d.Stairs);g.EnvironmentService.ReactElement(d.Stairs);
            Check(!w.Fire.ContainsKey(d.Stairs) && !w.Ice.ContainsKey(d.Stairs), "Reaction covered exit.");
            d.IsMerchantFloor=true;w.Oil.Add(oil);g.EnvironmentService.ReactElement(oil);
            Check(!w.Fire.ContainsKey(oil), "Reaction damaged safe merchant floor."); d.IsMerchantFloor=false;
            g.VisualEffects.Actions.Clear(); g.VisualEffects.Actions.PlayProjectile(g.PlayerState.Position,new[]{g.PlayerState.Position,p},true,choice);
            Check(g.VisualEffects.Actions.Animations.Single().Element==choice,"Projectile lost specialization element.");
        }
        w.HeroPoisonTurns=3;g.PlayerState.GuardTurns=2;
        Check(StatusText.For(g.PlayerState,null,d,g.Localization).Length>0,"Hero status label missing.");
        e.FrozenTurns=2;e.BlindTurns=2;w.PoisonedEnemies[e]=3;
        Check(StatusText.For(g.PlayerState,e,d,g.Localization).Contains(" / "),"Enemy statuses missing.");
        w.Clear();Check(w.Ice.Count==0 && w.Steam.Count==0,"New floor retained reactions.");
        d.Enemies.Clear(); g.PlayerState.AdvancedClass=AdvancedClass.Cryomancer;
        g.PlayerState.Energy=100; g.PlayerState.MaxEnergy=100; w.Details[p]='~';
        int turn = g.RunState.Turn;
        g.PlayerActions.Skill();
        Check(w.Ice.ContainsKey(p) && g.RunState.Turn==turn+1 && g.PlayerState.Energy<100, "Elemental Q cannot affect empty terrain or consumes no resources.");
        w.Clear(); w.Fire[p]=4;
        g.PlayerActions.Shoot(Vector2I.Right);
        Check(!w.Fire.ContainsKey(p) && w.Ice.ContainsKey(p), "Basic ice projectile did not react along its path.");
        w.Clear(); g.PlayerState.AdvancedClass=AdvancedClass.Pyromancer;w.Oil.Add(p);
        g.PlayerActions.Shoot(Vector2I.Right);
        Check(w.Fire.ContainsKey(p), "Basic fire projectile did not ignite oil.");
        GD.Print("ELEMENT AUDIT: fire/oil, ice/water, thaw/thermal shock, steam, frozen traps, expiry, exits, safe floors, elemental projectiles and portrait statuses passed.");
    }
}
