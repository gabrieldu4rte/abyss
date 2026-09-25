using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Abyss.Presentation;

internal enum ActionAnimationKind { Whirlwind, ArcaneNova, PiercingArrow, ShadowStep, ArcaneBolt, Arrow }
internal readonly record struct ActionGlyph(Vector2I Position, char Character, Color Color);

internal sealed class ActionAnimation
{
    internal ActionAnimationKind Kind { get; }
    internal Vector2I Origin { get; }
    internal IReadOnlyList<Vector2I> Path { get; }
    internal IReadOnlyList<Vector2I> Targets { get; }
    internal IReadOnlySet<Vector2I> VisibleCells { get; }
    internal int Radius { get; }
    internal double Elapsed { get; private set; }
    internal double TravelTime => Math.Clamp((Path.Count - 1) * .07, .18, .65);
    internal double Duration => Kind is ActionAnimationKind.Whirlwind or ActionAnimationKind.ArcaneNova ? .85 : TravelTime + .22;
    internal bool Finished => Elapsed >= Duration;

    internal ActionAnimation(ActionAnimationKind kind, Vector2I origin, IEnumerable<Vector2I> path,
        IEnumerable<Vector2I> targets, int radius, IReadOnlySet<Vector2I> visibleCells)
    {
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
        float fade = (float)Math.Clamp((Duration - Elapsed) / .22, 0, 1);
        var color = Kind switch
        {
            ActionAnimationKind.ArcaneNova or ActionAnimationKind.ArcaneBolt => new Color("b994ff"),
            ActionAnimationKind.ShadowStep => new Color("bd85de"),
            ActionAnimationKind.PiercingArrow => new Color("8ff0c5"),
            _ => new Color("ffd58a")
        };
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
                        glyph = "+*ox"[(Math.Abs(x) + Math.Abs(y) + (int)(Elapsed * 12)) % 4];
                    }
                    yield return new(Origin + new Vector2I(x, y), glyph, color);
                }
            if (Kind == ActionAnimationKind.ArcaneNova)
                foreach (var target in Targets)
                    if (Elapsed >= Math.Max(0, (GameRules.Dist(Origin, target) - .6) / Radius * .65))
                        yield return new(target, '*', color);
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
            yield return new(Path[head], Kind == ActionAnimationKind.ArcaneBolt ? '*' : Kind == ActionAnimationKind.ShadowStep ? '@' : arrow, color);
        else
        {
            var end = Path[^1];
            yield return new(end, Kind == ActionAnimationKind.ShadowStep ? '/' : '*', color);
            foreach (var d in GameRules.Directions)
                yield return new(end + d, Kind == ActionAnimationKind.ShadowStep ? '\\' : '+', color);
        }
    }
}
