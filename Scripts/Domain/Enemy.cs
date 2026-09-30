using Godot;
using System;
using System.Linq;
using static Abyss.Rules.TabletopRules;

namespace Abyss.Domain;
internal sealed class Enemy
{
    internal EliteTitle[] Titles { get; private set; } = Array.Empty<EliteTitle>();
    internal bool IsElite => Titles.Length > 0;
    internal void PromoteElite(params EliteTitle[] titles)
    {
        if (IsElite || Glyph == 'B' || titles.Length == 0) return;
        Titles = titles.Distinct().Take(2).ToArray();
        MaxHealth = (int)Math.Ceiling(MaxHealth * 1.4);
        Health = MaxHealth;
    }
    public Vector2I Position { get; set; }
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public int LastDamage { get; set; }
    public int Depth { get; set; }
    public double Hurt { get; set; }
    public bool Alerted { get; set; }
    public int SearchTurns { get; set; }
    public Vector2I? PatrolTarget { get; set; }
    public Vector2I? LastSeen { get; set; }
    public char Glyph { get; set; }
    public Attributes Stats { get; set; }
    public int Tier => (Depth - 1) / 5;
    public int Training => 1 + Tier / 2;
    public int CombatModifier => Glyph is 'r' or 'g' ? Stats.Dex : Stats.Str;
    public int AttackBonus => Training + CombatModifier + (IsElite ? 1 : 0);
    public int ArmorClass => (Glyph == 'B' ? 13 : Glyph == 's' ? 11 : 10) + Tier / 3;
    public int Armor => ArmorClass + Stats.Con + (IsElite ? 1 : 0);
    public DamageDice Dice => new(Glyph == 'B' ? 2 : 1, Glyph is 'B' or 'r' ? 3 : 4, CombatModifier + Tier * (Glyph == 'B' ? 2 : 1) + (IsElite ? 1 : 0));

    public Enemy(Vector2I p, char glyph, int depth)
    {
        Position = p;
        Glyph = glyph;
        Depth = depth;
        Stats = MonsterAttributes(glyph, depth);
        Health = MaxHealth = (glyph == 'B' ? 24 : glyph == 'g' ? 8 : glyph == 's' ? 6 : 4) + Tier * (glyph == 'B' ? 8 : 3);
    }
}
