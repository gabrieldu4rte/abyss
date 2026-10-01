using Godot;
using System;
using System.Linq;
namespace Abyss.Application;
internal sealed class HeroVitals(PlayerState player, InventoryState inventory, DungeonState dungeon, RunState run, VisualEffects effects, ExpeditionJournal journal)
{
    internal bool RescuePending { get; set; }
    internal int RescueSerial { get; private set; }
    internal int Heal(int amount, bool triggerRelic = true)
    {
        int gained = Math.Min(Math.Max(0, amount), Math.Max(0, player.MaxHealth - player.Health));
        player.Health += gained;
        if (gained == 0 || !triggerRelic || !inventory.Has(ItemId.SandflowerRelic)) return gained;
        foreach (var enemy in dungeon.Enemies.Where(e => GameRules.Dist(e.Position, player.Position) <= 3))
            enemy.BlindTurns = Math.Max(5, enemy.BlindTurns);
        for (int y = 1; y < GameRules.Height - 1; y++)
            for (int x = 1; x < GameRules.Width - 1; x++)
                if (dungeon.Tiles[x,y] == '#' && GameRules.Dist(new(x,y), player.Position) <= 3) dungeon.Explored[x,y] = true;
        effects.Actions.PlaySkill(1, player.Position, dungeon.Enemies.Where(e => e.BlindTurns > 0).Select(e => e.Position), 3);
        journal.Say("A Flor das Areias irradia luz: os inimigos ficam cegos!", "The Sandflower radiates light: nearby enemies are blinded!");
        return gained;
    }
    internal void Damage(int amount)
    {
        if (player.Health <= 0) return;
        player.Health = Math.Max(0, player.Health - Math.Max(0, amount));
        if (player.Health > 0) return;
        if (!inventory.HasLight && inventory.Equipped[1] is Gear { Special: ItemId.ShadowLegendsHood } hood)
        {
            var positions = new System.Collections.Generic.List<Vector2I>();
            for (int y = dungeon.StairsRoom.Position.Y; y < dungeon.StairsRoom.End.Y; y++)
                for (int x = dungeon.StairsRoom.Position.X; x < dungeon.StairsRoom.End.X; x++)
                {
                    var p = new Vector2I(x,y);
                    if (dungeon.Walk(p) && dungeon.At(p) == null) positions.Add(p);
                }
            if (positions.Count > 0)
            {
                var origin = player.Position;
                player.Position = positions.OrderBy(p => GameRules.Dist(p, dungeon.Stairs)).First();
                player.Health = 1; player.Energy = 0;
                inventory.Equipped[1] = null;
                inventory.Backpack.RemoveAll(g => ReferenceEquals(g, hood));
                RescueSerial++;
                RescuePending = true;
                effects.Actions.PlayShadowEscape(origin, player.Position);
                journal.Say("O capuz se desfaz em passaros sombrios e leva voce ate a escada!", "The hood breaks into shadow birds and carries you to the stairs!");
                return;
            }
        }
        run.Screen = "dead";
    }
}
