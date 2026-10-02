using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Abyss.Presentation;

internal enum ActionAnimationKind { Whirlwind, ArcaneNova, PiercingArrow, ShadowStep, ArcaneBolt, Arrow, Torch, SeismicImpact, FloodWave, SporeBurst, FurnaceCross, ShadowBirds, Bastion, CrushingBlow, Fireburst, FrozenPrison, ArrowRain, DeathMark, VenomBlade, ShadowVeil }
internal readonly record struct ActionGlyph(Vector2I Position, char Character, Color Color);

internal sealed class ActionAnimation
{
    internal AdvancedClass Element { get; }
    internal ActionAnimationKind Kind { get; }
    internal Vector2I Origin { get; }
    internal IReadOnlyList<Vector2I> Path { get; }
    internal IReadOnlyList<Vector2I> Targets { get; }
    internal IReadOnlySet<Vector2I> VisibleCells { get; }
    internal int Radius { get; }
    internal double Elapsed { get; private set; }
    internal double TravelTime => Math.Clamp((Path.Count - 1) * .07, .18, .65);
    internal double Duration => Kind >= ActionAnimationKind.SeismicImpact ? .95 : Kind is ActionAnimationKind.Whirlwind or ActionAnimationKind.ArcaneNova ? .85 : TravelTime + .22;
    internal bool Finished => Elapsed >= Duration;

    internal ActionAnimation(ActionAnimationKind kind, Vector2I origin, IEnumerable<Vector2I> path,
        IEnumerable<Vector2I> targets, int radius, IReadOnlySet<Vector2I> visibleCells, AdvancedClass element = AdvancedClass.None)
    {
        Element = element;
        Kind = kind;
        Origin = origin;
        Path = path.ToArray();
        Targets = targets.ToArray();
        Radius = radius;
        VisibleCells = visibleCells;
    }

    internal void Advance(double delta) => Elapsed += Math.Max(0, delta);

    internal IEnumerable<ActionGlyph> Sample()
    {
        if (Finished) yield break;
        if (Kind >= ActionAnimationKind.Bastion)
        {
            int phase = (int)(Elapsed * 12);
            var tint = new Color(Kind switch { ActionAnimationKind.Fireburst => "ff9955", ActionAnimationKind.FrozenPrison => "9deaff", ActionAnimationKind.VenomBlade => "a3d879", ActionAnimationKind.ShadowVeil => "ae8edb", _ => "f1d38b" });
            tint.A *= (float)Math.Clamp((Duration - Elapsed) / .25,0,1);
            string glyphs = Kind switch { ActionAnimationKind.Bastion => "[+#]", ActionAnimationKind.CrushingBlow => "#X+.", ActionAnimationKind.Fireburst => "^*x+", ActionAnimationKind.FrozenPrison => "+*#*", ActionAnimationKind.ArrowRain => "v|v!", ActionAnimationKind.DeathMark => ">-+*", ActionAnimationKind.VenomBlade => "/x%:", _ => "%:;~" };
            foreach (var target in Targets)
            {
                yield return new(target,glyphs[phase % glyphs.Length],tint);
                foreach (var offset in GameRules.Directions)
                    yield return new(target + offset,glyphs[(phase + 1) % glyphs.Length],tint);
            }
            if (Kind is ActionAnimationKind.ArrowRain or ActionAnimationKind.DeathMark or ActionAnimationKind.CrushingBlow)
                for (int i = 0; i < Path.Count; i++)
                    if (Math.Abs(i - (int)(Elapsed / Duration * Path.Count)) < 2) yield return new(Path[i],glyphs[phase % glyphs.Length],tint);
            yield break;
        }
        float fade = (float)Math.Clamp((Duration - Elapsed) / .22, 0, 1);
        var color = Kind switch
        {
            ActionAnimationKind.ArcaneNova or ActionAnimationKind.ArcaneBolt => new Color("b994ff"),
            ActionAnimationKind.ShadowBirds => new Color("b994ff"),
            ActionAnimationKind.FloodWave => new Color("70cfff"),
            ActionAnimationKind.SporeBurst => new Color("b5e87a"),
            ActionAnimationKind.FurnaceCross => new Color("ff8844"),
            ActionAnimationKind.Torch => new Color("ffac55"),
            ActionAnimationKind.ShadowStep => new Color("bd85de"),
            ActionAnimationKind.PiercingArrow => new Color("8ff0c5"),
            _ => new Color("ffd58a")
        };
        if (Element == AdvancedClass.Pyromancer) color = new Color("ff8844");
        if (Element == AdvancedClass.Cryomancer) color = new Color("9deaff");
        color.A = fade;
        if (Kind is ActionAnimationKind.Whirlwind or ActionAnimationKind.ArcaneNova)
        {
            double wave = Math.Min(Radius, .6 + Elapsed / .65 * Radius);
            for (int y = -Radius; y <= Radius; y++)
                for (int x = -Radius; x <= Radius; x++)
                {
                    int distance = Math.Abs(x) + Math.Abs(y);
                    if (distance == 0 || distance > Radius) continue;
                    char glyph;
                    if (Kind == ActionAnimationKind.Whirlwind)
                    {
                        double angle = Math.Atan2(y, x) - Elapsed * Math.PI * 5;
                        if (Math.Abs(Math.Cos(angle)) < .45) continue;
                        glyph = "/-\\|"[((int)(Elapsed * 16) + x + y + Radius * 2) % 4];
                    }
                    else
                    {
                        if (Math.Abs(distance - wave) > .8) continue;
                        glyph = (Element == AdvancedClass.Pyromancer ? "^*x^" : Element == AdvancedClass.Cryomancer ? "+*#*" : "+*ox")[(Math.Abs(x) + Math.Abs(y) + (int)(Elapsed * 12)) % 4];
                    }
                    yield return new(Origin + new Vector2I(x, y), glyph, color);
                }
            if (Kind == ActionAnimationKind.ArcaneNova)
                foreach (var target in Targets)
                    if (Elapsed >= Math.Max(0, (GameRules.Dist(Origin, target) - .6) / Radius * .65))
                        yield return new(target, '*', color);
            yield break;
        }

        if (Kind >= ActionAnimationKind.SeismicImpact)
        {
            int phase = (int)(Elapsed * 15);
            if (Kind == ActionAnimationKind.ShadowBirds)
            {
                foreach (var center in Targets)
                    for (int i = 0; i < 8; i++)
                    {
                        double angle = i * Math.PI / 4 + Elapsed * 3;
                        int radius = 1 + (int)(Elapsed * 3);
                        yield return new(center + new Vector2I((int)Math.Round(Math.Cos(angle)*radius), (int)Math.Round(Math.Sin(angle)*radius)), phase % 2 == 0 ? 'v' : '^', color);
                    }
                yield break;
            }
            foreach (var target in Targets)
            {
                int distance = GameRules.Dist(Origin, target);
                if (Kind != ActionAnimationKind.SporeBurst && Elapsed < distance * .065) continue;
                string ramp = Kind switch {
                    ActionAnimationKind.SeismicImpact => "#:+.",
                    ActionAnimationKind.FloodWave => "~=~-",
                    ActionAnimationKind.SporeBurst => "%*.:",
                    _ => "^*+^"
                };
                yield return new(target, ramp[(phase + distance + target.X + target.Y) % ramp.Length], color);
            }
            yield break;
        }

        int head = Math.Min(Path.Count - 1, 1 + (int)(Elapsed / TravelTime * (Path.Count - 1)));
        if (head < 0) yield break;
        var direction = head > 0 ? Path[head] - Path[head - 1] : Vector2I.Zero;
        char arrow = direction.X > 0 ? '>' : direction.X < 0 ? '<' : direction.Y > 0 ? 'v' : '^';
        char trail = Kind == ActionAnimationKind.ShadowStep ? ':' : Kind == ActionAnimationKind.ArcaneBolt ? '.' : direction.X == 0 ? '|' : '-';
        for (int i = Math.Max(0, head - 3); i < head; i++)
        {
            var tailColor = color;
            tailColor.A *= (i - head + 4) / 5f;
            yield return new(Path[i], trail, tailColor);
        }
        if (Elapsed < TravelTime)
            yield return new(Path[head], Kind is ActionAnimationKind.ArcaneBolt or ActionAnimationKind.Torch ? '*' : Kind == ActionAnimationKind.ShadowStep ? '@' : arrow, color);
        else
        {
            var end = Path[^1];
            yield return new(end, Kind == ActionAnimationKind.ShadowStep ? '/' : '*', color);
            foreach (var d in GameRules.Directions)
                yield return new(end + d, Kind == ActionAnimationKind.ShadowStep ? '\\' : '+', color);
        }
    }
}
