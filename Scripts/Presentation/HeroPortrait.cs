namespace Abyss.Presentation;

internal static class HeroPortrait
{
    internal static string Select(PlayerState player) =>
        player.MaxHealth > 0 && (long)player.Health * 4 <= player.MaxHealth
            ? AsciiArt.CriticalHeroes[player.ClassIndex]
            : AsciiArt.Heroes[player.ClassIndex];
}
