namespace Abyss.Rules;
internal static class EnemyTraits
{
    internal static int AttackRange(char glyph) => glyph switch { 's' => 5, 'i' => 4, 'b' => 3, _ => 1 };
    internal static bool Armored(char glyph) => glyph is 'q' or 'k';
    internal static bool Fireproof(char glyph) => glyph == 'a';
    internal static double PoisonChance(char glyph) => glyph switch { 'f' or 'm' => .25, 'b' => .35, _ => 0 };
}
