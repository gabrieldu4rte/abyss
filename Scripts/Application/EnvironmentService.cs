using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Abyss.Application;
internal sealed class EnvironmentService(DungeonState dungeon, InventoryState inventory, PlayerState player, RunState run, ExpeditionJournal journal, CombatService combat, VisualEffects effects, HeroVitals vitals)
{
    private EnvironmentState World => dungeon.Environment;
    internal bool Water(Vector2I p) => World.Details.TryGetValue(p, out var c) && c == '~';
    internal void Wet(Vector2I p)
    {
        if (!dungeon.Walk(p) || dungeon.IsSanctuary(p) || p == dungeon.Stairs) return;
        World.Fire.Remove(p);
        if (World.TemporaryWater.TryGetValue(p, out var existing)) World.TemporaryWater[p] = (2, existing.Original);
        else if (!Water(p)) World.TemporaryWater[p] = (2, World.Details.TryGetValue(p, out var old) ? old : null);
        World.Details[p] = '~';
    }
    internal bool Strike(Vector2I p)
    {
        if (!World.Fixtures.TryGetValue(p, out var fixture)) return false;
        if (fixture == Fixture.WallTorch)
        {
            effects.Sounds.Play("break");
            World.Fixtures.Remove(p);
            dungeon.Items[p] = 't';
            journal.Say("A tocha caiu. Pise nela para recolher.", "The torch fell. Step onto it to collect it.");
            return true;
        }
        if (fixture != Fixture.OilBarrel) return false;
        effects.Sounds.Play("break");
        SpillOil(p);
        if (World.Fire.Keys.Any(q => GameRules.Dist(q, p) <= 1)) Ignite(p);
        return true;
    }
    private void SpillOil(Vector2I p)
    {
        World.Fixtures.Remove(p);
        foreach (var q in new[] { p }.Concat(GameRules.Directions.Select(d => p + d)))
            if (dungeon.Walk(q) && !dungeon.IsSanctuary(q) && q != dungeon.Stairs && !Water(q)) World.Oil.Add(q);
        journal.Say("O barril tombou e espalhou oleo pelo chao.", "The barrel tipped over, spilling oil across the floor.");
    }
    internal void Ignite(Vector2I origin, bool report = true)
    {
        if (dungeon.IsSanctuary(origin) || dungeon.IsMerchantFloor || !dungeon.Walk(origin) || Water(origin)) return;
        var pending = new Queue<Vector2I>();
        var burning = new HashSet<Vector2I>();
        pending.Enqueue(origin);
        while (pending.Count > 0)
        {
            var p = pending.Dequeue();
            if (dungeon.IsSanctuary(p) || !burning.Add(p) || !dungeon.Walk(p) || Water(p) || p == dungeon.Stairs) continue;
            if (World.Fixtures.TryGetValue(p, out var fixture) && fixture == Fixture.OilBarrel) SpillOil(p);
            bool oil = World.Oil.Remove(p);
            World.Fire[p] = dungeon.Modifier == FloorModifier.HotDraft ? 6 : 4;
            foreach (var q in GameRules.Directions.Select(d => p + d))
            {
                if (World.Oil.Contains(q) || (oil && GameRules.Dist(q, origin) <= 2)) pending.Enqueue(q);
            }
        }
        if (report) journal.Say("As chamas se espalham! Afaste-se do oleo.", "Flames spread! Stay clear of the oil.");
    }
    internal bool ThrowTorch(Vector2I direction)
    {
        if (!inventory.HasLight && inventory.SpareTorches == 0) return false;
        var path = new List<Vector2I> { player.Position };
        var p = player.Position;
        bool piercing = inventory.Has(ItemId.ThrowingGauntlets);
        for (int i = 0; i < (piercing ? GameRules.Width + GameRules.Height : 5); i++)
        {
            var next = p + direction;
            if (!dungeon.Walk(next) || !dungeon.Visible[next.X, next.Y] || (dungeon.IsMerchantFloor && next == dungeon.MerchantPosition)) break;
            p = next; path.Add(p);
            if (!piercing && (World.Oil.Contains(p) || dungeon.At(p) != null || World.Fixtures.ContainsKey(p))) break;
        }
        if (path.Count == 1)
        {
            journal.Say("Nao ha espaco para arremessar nessa direcao.", "There is no room to throw in that direction.");
            return false;
        }
        if (inventory.HasLight) { inventory.TorchFuel = 0; inventory.TorchEquipped = false; inventory.LightReserve(); }
        else inventory.SpareTorches--;
        effects.Actions.PlayTorch(player.Position, path);
        if (piercing)
        {
            var hit = new HashSet<Enemy>();
            foreach (var cell in path.Skip(1))
            {
                var target = dungeon.At(cell);
                if (target != null && hit.Add(target))
                {
                    combat.Hit(target, 1);
                    if (target.Health > 0) EnemyDisplacement.Push(dungeon, player, target, player.Position);
                }
                if (World.Oil.Contains(cell) || World.Fixtures.TryGetValue(cell, out var fixture) && fixture == Fixture.OilBarrel) Ignite(cell);
            }
        }
        if (!Water(p)) Ignite(p);
        journal.Say(Water(p) ? "A agua apaga a tocha." : "Voce arremessa a tocha.", Water(p) ? "The water extinguishes the torch." : "You throw the torch.");
        return true;
    }
    internal void Tick()
    {
        int rescue = vitals.RescueSerial;
        bool burning = inventory.HasLight;
        if (burning) inventory.BurnTorchTurn();
        if (burning && inventory.TorchFuel == 0)
        {
            inventory.TorchEquipped = false;
            if (inventory.LightReserve())
                journal.Say("Sua tocha acabou. Voce acende a proxima reserva.", "Your torch burned out. You light the next spare.");
            else
                journal.Say("Sua ultima tocha se apagou.", "Your last torch burned out.");
        }
        TriggerTrap(player.Position, null);
        if (player.Health <= 0 || vitals.RescueSerial != rescue) return;
        foreach (var enemy in dungeon.Enemies.ToArray())
        {
            if (player.Health <= 0 || vitals.RescueSerial != rescue) return;
            if (enemy.Health > 0 && dungeon.Enemies.Contains(enemy)) TriggerTrap(enemy.Position, enemy);
        }
        if (World.Fire.ContainsKey(player.Position)) HurtHero(Math.Max(0, FireDamage - (inventory.Has(ItemId.InsulatingLeather) ? 1 : 0)), "Fogo", "Fire");
        if (vitals.RescueSerial != rescue) return;
        if (World.HeroPoisonTurns > 0 && player.Health > 0)
        {
            HurtHero(2, "Veneno", "Poison"); World.HeroPoisonTurns--;
        }
        if (player.Health <= 0 || vitals.RescueSerial != rescue) return;
        foreach (var enemy in dungeon.Enemies.ToArray())
        {
            int damage = World.Fire.ContainsKey(enemy.Position) ? FireDamage : 0;
            if (World.PoisonedEnemies.TryGetValue(enemy, out int remaining))
            {
                damage += 2;
                if (remaining <= 1) World.PoisonedEnemies.Remove(enemy); else World.PoisonedEnemies[enemy] = remaining - 1;
            }
            if (damage == 0) continue;
            journal.Say($"Ambiente -> {FloorEventText.EnemyName(enemy, false)}: {damage} dano.", $"Environment -> {FloorEventText.EnemyName(enemy, true)}: {damage} damage.");
            combat.Hit(enemy, damage, false);
        }
        foreach (var enemy in World.PoisonedEnemies.Keys.Where(e => !dungeon.Enemies.Contains(e)).ToArray()) World.PoisonedEnemies.Remove(enemy);
        foreach (var p in World.TemporaryWater.Keys.ToArray())
        {
            var water = World.TemporaryWater[p];
            if (water.Turns > 1) World.TemporaryWater[p] = (water.Turns - 1, water.Original);
            else
            {
                if (water.Original is char original) World.Details[p] = original; else World.Details.Remove(p);
                World.TemporaryWater.Remove(p);
            }
        }
        foreach (var p in World.Fire.Keys.ToArray())
            if (--World.Fire[p] <= 0 || Water(p)) World.Fire.Remove(p);
    }
    private int FireDamage => 3 + Math.Min(5, GameRules.CycleIndex(dungeon.Floor));
    private void TriggerTrap(Vector2I p, Enemy? enemy)
    {
        if (!World.Fixtures.TryGetValue(p, out var fixture) || !TrapRules.IsTrap(fixture)) return;
        int rescue = vitals.RescueSerial;
        effects.Sounds.Play("trap");
        World.Fixtures[p] = Fixture.SpentTrap;
        int bonus = Math.Min(4, GameRules.CycleIndex(dungeon.Floor));
        switch (fixture)
        {
            case Fixture.PoisonTrap:
                if (enemy == null) World.HeroPoisonTurns = 3; else World.PoisonedEnemies[enemy] = 3;
                journal.Say("Esporos venenosos escapam da armadilha!", "Poisonous spores escape the trap!");
                break;
            case Fixture.SpikeTrap:
                journal.Say("Espinhos saltam do piso!", "Spikes spring from the floor!");
                if (enemy == null) HurtHero(5 + bonus, "Espinhos", "Spikes");
                else combat.Hit(enemy, 5 + bonus, false);
                break;
            case Fixture.ShockTrap:
                journal.Say("Uma descarga atinge as casas adjacentes!", "A discharge strikes adjacent cells!");
                if ((player.Position == p || !inventory.Has(ItemId.InsulatingLeather)) && GameRules.Dist(player.Position, p) <= 1 && dungeon.Los(p, player.Position)) HurtHero(3 + bonus, "Descarga", "Discharge");
                if (player.Health <= 0 || vitals.RescueSerial != rescue) return;
                foreach (var target in dungeon.Enemies.Where(e => GameRules.Dist(e.Position, p) <= 1 && dungeon.Los(p, e.Position)).ToArray()) combat.Hit(target, 3 + bonus, false);
                break;
            case Fixture.FlameTrap:
                journal.Say("Um jato de fogo irrompe do piso!", "A jet of flame erupts from the floor!");
                foreach (var q in new[] { p }.Concat(GameRules.Directions.Select(d => p + d)))
                    if (dungeon.Walk(q) && !dungeon.IsSanctuary(q) && q != dungeon.Stairs && !Water(q)) Ignite(q, false);
                break;
        }
    }
    private void HurtHero(int damage, string pt, string en)
    {
        if (player.Health <= 0 || dungeon.IsSanctuary(player.Position)) return;
        vitals.Damage(damage);
        effects.Sounds.Play("herohurt");
        effects.HeroHurtRemaining = UiTheme.HurtDuration;
        effects.HeroDamage = damage;
        effects.Effects.Add(new DamageEffect { Position = player.Position, Damage = damage, Hero = true });
        journal.Say($"{pt}: -{damage} PV.", $"{en}: -{damage} HP.");
        if (player.Health == 0) run.Screen = "dead";
    }
}
