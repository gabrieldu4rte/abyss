using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Abyss.Presentation;

internal sealed class ActionEffects(DungeonState dungeonState, SoundEffects sounds)
{
    private readonly List<ActionAnimation> animations = new();
    internal IReadOnlyList<ActionAnimation> Animations => animations;
    internal bool Active => animations.Count > 0;
    internal void Clear() => animations.Clear();
    internal void Advance(double delta)
    {
        foreach (var animation in animations) animation.Advance(delta);
        animations.RemoveAll(animation => animation.Finished);
    }

    internal void PlayProjectile(Vector2I origin, IReadOnlyList<Vector2I> path, bool arcane)
        => Add(arcane ? ActionAnimationKind.ArcaneBolt : ActionAnimationKind.Arrow, origin, path, new Vector2I[0], 0);

    internal void PlayTorch(Vector2I origin, IReadOnlyList<Vector2I> path)
        => Add(ActionAnimationKind.Torch, origin, path, new Vector2I[0], 0);

    internal void PlaySkill(int classIndex, Vector2I origin, IEnumerable<Vector2I> targets, int radius)
    {
        var positions = targets.ToArray();
        var path = classIndex < 2 || positions.Length == 0 ? new[] { origin } : TraceLine(origin, positions[0]);
        Add((ActionAnimationKind)classIndex, origin, path, positions, radius);
    }

    internal void PlayWarden(Biome biome, Vector2I origin, IEnumerable<Vector2I> cells)
        => Add((ActionAnimationKind)((int)ActionAnimationKind.SeismicImpact + (int)biome), origin, new[] { origin }, cells, 5);

    internal void PlayShadowEscape(Vector2I origin, Vector2I destination)
        => Add(ActionAnimationKind.ShadowBirds, origin, new[] { origin }, new[] { origin, destination }, 3);

    private void Add(ActionAnimationKind kind, Vector2I origin, IEnumerable<Vector2I> path, IEnumerable<Vector2I> targets, int radius)
    {
        sounds.Play(kind == ActionAnimationKind.ShadowBirds ? "shadowstep" : kind.ToString().ToLowerInvariant());
        var visible = new HashSet<Vector2I>();
        for (int y = 1; y < GameRules.Height - 1; y++)
            for (int x = 1; x < GameRules.Width - 1; x++)
            {
                var position = new Vector2I(x, y);
                if (dungeonState.Visible[x, y] && dungeonState.Walk(position)) visible.Add(position);
            }
        if (animations.Count >= 24) animations.RemoveAt(0);
        animations.Add(new ActionAnimation(kind, origin, path, targets, radius, visible));
    }

    private static Vector2I[] TraceLine(Vector2I origin, Vector2I target)
    {
        var path = new List<Vector2I> { origin };
        int x = origin.X, y = origin.Y, dx = System.Math.Abs(target.X - x), dy = -System.Math.Abs(target.Y - y);
        int sx = x < target.X ? 1 : -1, sy = y < target.Y ? 1 : -1, error = dx + dy;
        while (x != target.X || y != target.Y)
        {
            int twice = error * 2;
            if (twice >= dy) { error += dy; x += sx; }
            if (twice <= dx) { error += dx; y += sy; }
            path.Add(new Vector2I(x, y));
        }
        return path.ToArray();
    }
}
