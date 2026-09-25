namespace Abyss.Domain;
internal sealed class RunState
{
    internal int Turn { get; set; }
    internal int Seed { get; set; }
    internal string Screen { get; set; } = "home";
    internal bool IsAiming { get; set; }
}
