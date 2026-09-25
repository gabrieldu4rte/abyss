using System;
using System.Linq;

namespace Abyss.Rules;
// All probability and dice rules are deterministic when supplied a seeded Random.
public static class TabletopRules
{
    public readonly record struct Attributes(int Strength, int Dexterity, int Constitution, int Intelligence)
    {
        public int Str => Modifier(Strength);
        public int Dex => Modifier(Dexterity);
        public int Con => Modifier(Constitution);
        public int Int => Modifier(Intelligence);
    }

    public readonly record struct DamageDice(int Count, int Sides, int Bonus)
    {
        public override string ToString() => $"{Count}d{Sides}" + (Bonus == 0 ? "" : Bonus > 0 ? $"+{Bonus}" : $"{Bonus}");
        public double Average => Count * (Sides + 1) / 2.0 + Bonus;
    }

    public readonly record struct AttackRoll(int Natural, int Bonus, int Defense, bool Hit, bool Critical)
    {
        public int Total => Natural + Bonus;
    }

    public static int Modifier(int score) => (int)Math.Floor((score - 10) / 2.0);
    public static AttackRoll ResolveAttack(int natural, int bonus, int defense, int criticalAt = 20)
    {
        bool critical = natural >= criticalAt;
        return new(natural, bonus, defense, natural != 1 && (critical || natural + bonus >= defense), critical && natural != 1);
    }

    public static int HitChance(int bonus, int defense, int criticalAt = 20) => Enumerable.Range(1, 20).Count(n => ResolveAttack(n, bonus, defense, criticalAt).Hit) * 5;
    public static int RollDamage(Random random, DamageDice dice, bool critical = false)
    {
        int result = dice.Bonus;
        for (int i = 0; i < dice.Count * (critical ? 2 : 1); i++)
            result += random.Next(1, dice.Sides + 1);
        return Math.Max(1, result);
    }

    public static (int First, int Second, int Total) RollPotion(Random random)
    {
        int a = random.Next(1, 11), b = random.Next(1, 11);
        return (a, b, a + b);
    }

    public static Attributes HeroAttributes(int hero) => hero switch
    {
        0 => new(15, 10, 14, 8),
        1 => new(8, 12, 10, 15),
        2 => new(10, 15, 12, 10),
        _ => new(11, 15, 11, 12)};
    public static Attributes MonsterAttributes(char glyph, int depth)
    {
        var baseStats = glyph switch
        {
            'r' => new Attributes(8, 10, 8, 2),
            's' => new(10, 10, 10, 4),
            'g' => new(10, 12, 10, 8),
            _ => new(12, 10, 12, 12)};
        // Keep combat attributes stable within each five-floor band.
        int growth = (depth - 1) / 5;
        return new(baseStats.Strength + growth, baseStats.Dexterity + growth / 2, baseStats.Constitution + growth, baseStats.Intelligence + growth / 2);
    }
}
