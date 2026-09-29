namespace Abyss.Domain;
internal sealed class Offer
{
    public Gear? Gear { get; set; }
    public int Potion { get; set; }
    public int Quantity { get; set; } = 1;
    public int Value => Gear?.Value ?? (Potion == 0 ? 12 : Potion == 1 ? 15 : 8);
}
