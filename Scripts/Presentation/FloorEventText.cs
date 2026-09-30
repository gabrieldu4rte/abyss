using Godot;
using System.Linq;

namespace Abyss.Presentation;
internal static class FloorEventText
{
    internal static string Name(FloorModifier modifier, bool english) => (english ? new[] { "", "LIGHTLESS DEPTHS", "INFESTATION", "HOT DRAFT", "THIN AIR", "HIDDEN CACHES" } : new[] { "", "TREVAS PROFUNDAS", "INFESTACAO", "VENTOS QUENTES", "AR RAREFEITO", "TESOUROS OCULTOS" })[(int)modifier];
    internal static string Description(FloorModifier modifier, bool english) => (english ? new[] {
        "", "Darkness smothers all light. Sight is limited to two cells.", "One kind of creature has overrun these halls.", "Hot drafts feed the flames: fire lasts six turns.", "Thin air: natural energy recovery takes twice as long.", "Forgotten treasure awaits: two extra chests may be found."
    } : new[] {
        "", "As trevas abafam toda luz. Visao limitada a duas casas.", "Um unico tipo de criatura tomou estes saloes.", "Ventos quentes alimentam as chamas: fogo dura seis turnos.", "Ar rarefeito: a recuperacao natural de energia demora o dobro.", "Tesouros esquecidos: ate dois baus extras esperam por voce."
    })[(int)modifier];
    internal static string Title(EliteTitle title, bool english) => (english ? new[] { "CRUEL", "IRONBOUND", "RELENTLESS", "DREAD", "PROFANE", "ANCIENT" } : new[] { "CRUEL", "BLINDADO", "IMPLACAVEL", "SOMBRIO", "PROFANO", "ANCESTRAL" })[(int)title];
    internal static Color TitleColor(EliteTitle title) => new(new[] { "f08070", "86b4e0", "edbd6a", "b695ee", "82caa2", "e9a3d4" }[(int)title]);
    internal static string EnemyName(Enemy enemy, bool english) => Localization.MonsterName(enemy.Glyph, english) + (enemy.IsElite ? " [ELITE: " + string.Join(" / ", enemy.Titles.Select(t => Title(t, english))) + "]" : "");
}
