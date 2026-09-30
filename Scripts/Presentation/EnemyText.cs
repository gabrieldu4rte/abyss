namespace Abyss.Presentation;
internal static class EnemyText
{
    internal static string Name(char glyph, int depth, bool english) => glyph == 'B'
        ? (english ? new[] { "Ruin Warden", "Tide Warden", "Spore Sovereign", "Forge Warden" } : new[] { "Guardiao das Ruinas", "Guardiao das Mares", "Soberano dos Esporos", "Guardiao da Forja" })[(int)EnemyCatalog.BiomeAt(depth)]
        : glyph switch
        {
            's' => english ? "Skeleton" : "Esqueleto", 'g' => "Goblin", 'r' => english ? "Crypt rat" : "Rato das criptas",
            'v' => english ? "Revenant" : "Revenante", 'd' => english ? "Drowned" : "Afogado", 'l' => english ? "Giant leech" : "Sanguessuga gigante",
            'f' => english ? "Sporeling" : "Esporito", 'j' => english ? "Cave crawler" : "Rastejante", 'm' => english ? "Myconid" : "Miconide",
            'h' => english ? "Cinder hound" : "Cao das cinzas", 'i' => english ? "Ember imp" : "Diabrete das brasas", 'k' => english ? "Forged sentinel" : "Sentinela de ferro",
            _ => english ? "No target" : "Sem alvo"
        };
    internal static string Ability(Biome biome, bool english) => (english ? new[] { "Seismic impact", "Flood wave", "Spore burst", "Furnace cross" } : new[] { "Impacto sismico", "Onda de inundacao", "Explosao de esporos", "Cruz da fornalha" })[(int)biome];
}
