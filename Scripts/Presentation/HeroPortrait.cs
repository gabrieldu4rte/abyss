namespace Abyss.Presentation;
internal static class HeroPortrait
{
    internal static string Select(PlayerState player)
    {
        bool critical = player.MaxHealth > 0 && (long)player.Health * 4 <= player.MaxHealth;
        if (player.AdvancedClass != AdvancedClass.None) return AsciiArt.AdvancedHero(player.AdvancedClass,critical);
        return critical ? AsciiArt.CriticalHeroes[player.ClassIndex] : AsciiArt.Heroes[player.ClassIndex];
    }
}
