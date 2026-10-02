using System;
namespace Abyss.Application;
internal sealed class EnemySpecialEffects(PlayerState player, InventoryState inventory, DungeonState dungeon, EnvironmentService environment, RandomStream random, ExpeditionJournal journal)
{
    internal void OnHit(Enemy enemy)
    {
        if (player.Health <= 0 || dungeon.IsSanctuary(player.Position)) return;
        double poison = EnemyTraits.PoisonChance(enemy.Glyph);
        if (poison > 0 && !inventory.Has(ItemId.FungalSovereignCrown) && random.Generator.NextDouble() < poison)
        {
            dungeon.Environment.HeroPoisonTurns = Math.Max(3, dungeon.Environment.HeroPoisonTurns);
            journal.Say("O golpe libera esporos venenosos!", "The strike releases poisonous spores!");
        }
        if (enemy.Glyph == 'l')
        {
            int healed = Math.Min(2, enemy.MaxHealth - enemy.Health); enemy.Health += healed;
            if (healed > 0) journal.Say("A sanguessuga recupera vida ao se alimentar.", "The leech recovers health as it feeds.");
        }
        if (enemy.Glyph == 'e' && environment.Water(player.Position) && !inventory.Has(ItemId.ThickRubberBoots))
        {
            player.Energy = Math.Max(0, player.Energy - 2);
            journal.Say("A enguia descarrega na agua: -2 energia.", "The eel discharges into the water: -2 energy.");
        }
        if (enemy.Glyph is 'i' or 'a' && random.Generator.NextDouble() < .25)
        {
            environment.Ignite(player.Position, false);
            journal.Say("Brasas se espalham pelo chao.", "Embers scatter across the floor.");
        }
    }
}
