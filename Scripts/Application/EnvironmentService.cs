using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Abyss.Application;
internal sealed class EnvironmentService(DungeonState dungeon, InventoryState inventory, PlayerState player, RunState run, ExpeditionJournal journal, CombatService combat, VisualEffects effects)
{
    private EnvironmentState World => dungeon.Environment;
    internal bool Water(Vector2I p) => World.Details.TryGetValue(p, out var c) && c == '~';
    internal bool Strike(Vector2I p)
    {
        if (!World.Fixtures.TryGetValue(p, out var fixture)) return false;
        if (fixture == Fixture.WallTorch)
        {
            World.Fixtures.Remove(p);
            dungeon.Items[p] = 't';
            journal.Say("A tocha caiu. Pise nela para recolher.", "The torch fell. Step onto it to collect it.");
            return true;
        }
        if (fixture != Fixture.OilBarrel) return false;
        SpillOil(p);
        if (World.Fire.Keys.Any(q => GameRules.Dist(q, p) <= 1)) Ignite(p);
        return true;
    }
    private void SpillOil(Vector2I p)
    {
        World.Fixtures.Remove(p);
        foreach (var q in new[] { p }.Concat(GameRules.Directions.Select(d => p + d)))
            if (dungeon.Walk(q) && q != dungeon.Stairs && !Water(q)) World.Oil.Add(q);
        journal.Say("O barril tombou e espalhou oleo pelo chao.", "The barrel tipped over, spilling oil across the floor.");
    }
    internal void Ignite(Vector2I origin)
    {
        if (dungeon.IsMerchantFloor || !dungeon.Walk(origin) || Water(origin)) return;
        var pending = new Queue<Vector2I>();
        var burning = new HashSet<Vector2I>();
        pending.Enqueue(origin);
        while (pending.Count > 0)
        {
            var p = pending.Dequeue();
            if (!burning.Add(p) || !dungeon.Walk(p) || Water(p) || p == dungeon.Stairs) continue;
            if (World.Fixtures.TryGetValue(p, out var fixture) && fixture == Fixture.OilBarrel) SpillOil(p);
            bool oil = World.Oil.Remove(p);
            World.Fire[p] = 4;
            foreach (var q in GameRules.Directions.Select(d => p + d))
            {
                if (World.Oil.Contains(q) || (oil && GameRules.Dist(q, origin) <= 2)) pending.Enqueue(q);
            }
        }
        journal.Say("As chamas se espalham! Afaste-se do oleo.", "Flames spread! Stay clear of the oil.");
    }
    internal bool ThrowTorch(Vector2I direction)
    {
        if (!inventory.HasLight && inventory.SpareTorches == 0) return false;
        var path = new List<Vector2I> { player.Position };
        var p = player.Position;
        for (int i = 0; i < 5; i++)
        {
            var next = p + direction;
            if (!dungeon.Walk(next) || !dungeon.Visible[next.X, next.Y] || (dungeon.IsMerchantFloor && next == dungeon.MerchantPosition)) break;
            p = next; path.Add(p);
            if (World.Oil.Contains(p) || dungeon.At(p) != null || World.Fixtures.ContainsKey(p)) break;
        }
        if (path.Count == 1)
        {
            journal.Say("Nao ha espaco para arremessar nessa direcao.", "There is no room to throw in that direction.");
            return false;
        }
        if (inventory.HasLight) { inventory.TorchFuel = 0; inventory.TorchEquipped = false; }
        else inventory.SpareTorches--;
        effects.Actions.PlayTorch(player.Position, path);
        if (!Water(p)) Ignite(p);
        journal.Say(Water(p) ? "A agua apaga a tocha." : "Voce arremessa a tocha.", Water(p) ? "The water extinguishes the torch." : "You throw the torch.");
        return true;
    }
    internal void Tick()
    {
        if (inventory.HasLight && --inventory.TorchFuel == 0)
        {
            inventory.TorchEquipped = false;
            journal.Say("Sua tocha se apagou. Acenda outra no inventario.", "Your torch burned out. Light another in your inventory.");
        }
        TriggerTrap(player.Position, null);
        foreach (var enemy in dungeon.Enemies.ToArray()) TriggerTrap(enemy.Position, enemy);
        if (World.Fire.ContainsKey(player.Position)) HurtHero(FireDamage, "Fogo", "Fire");
        if (World.HeroPoisonTurns > 0 && player.Health > 0)
        {
            HurtHero(2, "Veneno", "Poison"); World.HeroPoisonTurns--;
        }
        if (player.Health <= 0) return;
        foreach (var enemy in dungeon.Enemies.ToArray())
        {
            int damage = World.Fire.ContainsKey(enemy.Position) ? FireDamage : 0;
            if (World.PoisonedEnemies.TryGetValue(enemy, out int remaining))
            {
                damage += 2;
                if (remaining <= 1) World.PoisonedEnemies.Remove(enemy); else World.PoisonedEnemies[enemy] = remaining - 1;
            }
            if (damage == 0) continue;
            journal.Say($"Ambiente -> {Localization.MonsterName(enemy.Glyph, false)}: {damage} dano.", $"Environment -> {Localization.MonsterName(enemy.Glyph, true)}: {damage} damage.");
            combat.Hit(enemy, damage, false);
        }
        foreach (var enemy in World.PoisonedEnemies.Keys.Where(e => !dungeon.Enemies.Contains(e)).ToArray()) World.PoisonedEnemies.Remove(enemy);
        foreach (var p in World.Fire.Keys.ToArray())
            if (--World.Fire[p] <= 0 || Water(p)) World.Fire.Remove(p);
    }
    private int FireDamage => 3 + Math.Min(5, GameRules.CycleIndex(dungeon.Floor));
    private void TriggerTrap(Vector2I p, Enemy? enemy)
    {
        if (!World.Fixtures.TryGetValue(p, out var fixture) || fixture != Fixture.PoisonTrap) return;
        World.Fixtures[p] = Fixture.SpentTrap;
        if (enemy == null) World.HeroPoisonTurns = 3; else World.PoisonedEnemies[enemy] = 3;
        journal.Say("Uma armadilha libera veneno!", "A trap releases poison!");
    }
    private void HurtHero(int damage, string pt, string en)
    {
        if (player.Health <= 0) return;
        player.Health = Math.Max(0, player.Health - damage);
        effects.HeroHurtRemaining = UiTheme.HurtDuration;
        effects.HeroDamage = damage;
        effects.Effects.Add(new DamageEffect { Position = player.Position, Damage = damage, Hero = true });
        journal.Say($"{pt}: -{damage} PV.", $"{en}: -{damage} HP.");
        if (player.Health == 0) run.Screen = "dead";
    }
}
