using Godot;
using static Abyss.Rules.TabletopRules;

namespace Abyss.Domain;
internal sealed class Enemy
{
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
    public int AttackBonus => Training + CombatModifier;
    public int ArmorClass => (Glyph == 'B' ? 13 : Glyph == 's' ? 11 : 10) + Tier / 3;
    public int Armor => ArmorClass + Stats.Con;
    public DamageDice Dice => new(Glyph == 'B' ? 2 : 1, Glyph is 'B' or 'r' ? 3 : 4, CombatModifier + Tier * (Glyph == 'B' ? 2 : 1));

    public Enemy(Vector2I p, char glyph, int depth)
    {
        Position = p;
        Glyph = glyph;
        Depth = depth;
        Stats = MonsterAttributes(glyph, depth);
        Health = MaxHealth = (glyph == 'B' ? 24 : glyph == 'g' ? 8 : glyph == 's' ? 6 : 4) + Tier * (glyph == 'B' ? 8 : 3);
    }
}
