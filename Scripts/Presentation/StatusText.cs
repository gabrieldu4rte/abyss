using Godot;
using System.Collections.Generic;
namespace Abyss.Presentation;
internal static class StatusText
{
    internal static string For(PlayerState hero, Enemy? enemy, DungeonState dungeon, Localization text)
    {
        var labels = new List<string>();
        var p = enemy?.Position ?? hero.Position;
        if (enemy != null && enemy.Health <= 0 || enemy == null && hero.Health <= 0) return "";
        if (enemy?.FrozenTurns > 0) labels.Add(text.Translate("CONGELADO", "FROZEN"));
        if (enemy?.BlindTurns > 0) labels.Add(text.Translate("CEGO", "BLIND"));
        if (enemy == null ? dungeon.Environment.HeroPoisonTurns > 0 : dungeon.Environment.PoisonedEnemies.ContainsKey(enemy)) labels.Add(text.Translate("VENENO", "POISON"));
        if (dungeon.Environment.Fire.ContainsKey(p)) labels.Add(text.Translate("EM CHAMAS", "BURNING"));
        if (enemy == null && hero.GuardTurns > 0) labels.Add(text.Translate("PROTEGIDO", "GUARDED"));
        return string.Join(" / ", labels);
    }
    internal static string Basic(PlayerState hero, Localization text) => hero.AdvancedClass switch
    {
        AdvancedClass.Pyromancer => text.Translate("Dardo de fogo", "Fire bolt"),
        AdvancedClass.Cryomancer => text.Translate("Dardo de gelo", "Ice bolt"),
        _ => text.Translate("Basico", "Basic")
    };
}
