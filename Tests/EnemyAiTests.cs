using Godot;
using System;
using System.Linq;

namespace Abyss.Tests;
internal sealed partial class RegressionSuite
{
    internal void TestEnemyAi()
    {
        game.PlayerState.ClassIndex = 0;
        game.Start(91);
        game.DungeonState.Enemies.Clear();
        game.RandomGenerator = new RegressionSuite.FixedRandom(1);
        for (int x = 0; x < GameRules.Width; x++)
            for (int y = 0; y < GameRules.Height; y++)
                game.DungeonState.Tiles[x, y] = x == 0 || y == 0 || x == GameRules.Width - 1 || y == GameRules.Height - 1 ? '#' : '.';
        game.PlayerState.Position = new Vector2I(50, 20);
        var scout = new Enemy(new Vector2I(3, 4), 'g', 1)
        {
            PatrolTarget = new Vector2I(8, 4)
        };
        game.DungeonState.Enemies.Add(scout);
        game.EnemyAi.ActEnemy(scout);
        if (scout.Position != new Vector2I(4, 4) || scout.Alerted)
            throw new Exception("Patrol did not move while unaware");
        game.PlayerState.Position = new Vector2I(8, 4);
        game.EnemyAi.ActEnemy(scout);
        if (!scout.Alerted || scout.Position != new Vector2I(5, 4) || scout.LastSeen != game.PlayerState.Position)
            throw new Exception("Visible player not pursued immediately");
        for (int y = 1; y <= 15; y++)
            game.DungeonState.Tiles[10, y] = '#';
        game.PlayerState.Position = new Vector2I(12, 4);
        game.EnemyAi.ActEnemy(scout);
        if (scout.LastSeen != new Vector2I(8, 4) || scout.Position != new Vector2I(6, 4))
            throw new Exception("Enemy tracked player through wall");
        for (int i = 0; i < 8; i++)
            game.EnemyAi.ActEnemy(scout);
        if (scout.Alerted)
            throw new Exception("Search memory never expired");
        game.DungeonState.Enemies.Clear();
        scout = new Enemy(new Vector2I(8, 4), 'r', 1);
        game.DungeonState.Enemies.Add(scout);
        game.PlayerState.Position = new Vector2I(50, 20);
        for (int i = 0; i < 40 && scout.Position != new Vector2I(12, 4); i++)
            game.EnemyNavigator.StepEnemy(scout, new Vector2I(12, 4));
        if (scout.Position != new Vector2I(12, 4))
            throw new Exception("Pathfinding failed to route around wall");
        var blocker = new Enemy(new Vector2I(13, 4), 's', 1);
        game.DungeonState.Enemies.Add(blocker);
        if (game.EnemyNavigator.StepEnemy(scout, blocker.Position) || scout.Position == blocker.Position)
            throw new Exception("Enemies overlapped");
        game.DungeonState.Enemies.Clear();
        game.DungeonState.StairsRoom = new Rect2I(20, 10, 6, 5);
        game.DungeonState.Stairs = new Vector2I(23, 12);
        var boss = new Enemy(new Vector2I(20, 12), 'B', 5);
        game.DungeonState.Enemies.Add(boss);
        game.PlayerState.Position = new Vector2I(19, 12);
        game.PlayerState.Health = game.PlayerState.MaxHealth;
        int beforeHp = game.PlayerState.Health;
        game.EnemyAi.ActEnemy(boss);
        if (boss.Alerted || boss.Position != new Vector2I(20, 12) || game.PlayerState.Health != beforeHp)
            throw new Exception("Boss activated outside its room");
        game.PlayerState.Position = new Vector2I(21, 12);
        game.EnemyAi.ActEnemy(boss);
        if (!boss.Alerted || !game.ExpeditionJournal.Entries.Any(e => e.En.Contains("Warden awakens")))
            throw new Exception("Boss did not activate on room entry");
        game.PlayerState.Position = new Vector2I(19, 12);
        beforeHp = game.PlayerState.Health;
        for (int i = 0; i < 15; i++)
        {
            game.EnemyAi.ActEnemy(boss);
            if (!game.DungeonState.StairsRoom.HasPoint(boss.Position) || game.PlayerState.Health != beforeHp)
                throw new Exception("Boss left room or attacked outside it");
        }

        if (boss.Position != game.DungeonState.Stairs)
            throw new Exception("Boss did not return to stairs");
        game.PlayerState.Position = new Vector2I(25, 14);
        game.EnemyAi.ActEnemy(boss);
        if (boss.Position == game.DungeonState.Stairs)
            throw new Exception("Boss did not resume pursuit after reentry");
        for (int seed = 1; seed <= 20; seed++)
        {
            game.Start(seed);
            game.DungeonState.Floor = 10;
            game.DungeonGenerator.Generate();
            game.PlayerState.Health = 100000;
            var guardian = game.DungeonState.Enemies.Single(e => e.Glyph == 'B');
            for (int i = 0; i < 30; i++)
            {
                game.EndTurn();
                if (game.DungeonState.Enemies.Any(e => !game.DungeonState.Walk(e.Position) || e.Position == game.PlayerState.Position) || game.DungeonState.Enemies.Select(e => e.Position).Distinct().Count() != game.DungeonState.Enemies.Count)
                    throw new Exception("Patrol movement violated occupancy");
                if (guardian.Alerted || guardian.Position != game.DungeonState.Stairs)
                    throw new Exception("Distant boss activated during patrol");
            }
        }

        game.PlayerState.ClassIndex = 0;
        game.Start(50);
        game.DungeonState.Enemies.Clear();
        game.RandomGenerator = new RegressionSuite.FixedRandom(20);
        for (int x = 1; x < GameRules.Width - 1; x++)
            for (int y = 1; y < GameRules.Height - 1; y++)
                game.DungeonState.Tiles[x, y] = '.';
        game.PlayerState.Position = new Vector2I(5, 5);
        var melee = new Enemy(new Vector2I(7, 5), 'r', 1);
        game.DungeonState.Enemies.Add(melee);
        int health = game.PlayerState.Health;
        game.CombatService.ResolveEnemyAttack(melee, false);
        if (game.PlayerState.Health != health)
            throw new Exception("Enemy attacked from two tiles away");
        melee.Position = new Vector2I(6, 6);
        game.CombatService.ResolveEnemyAttack(melee, false);
        if (game.PlayerState.Health != health)
            throw new Exception("Enemy attacked diagonally");
        melee.Position = new Vector2I(7, 5);
        game.PlayerActions.Move(Vector2I.Right);
        if (game.PlayerState.Health != health || game.PlayerState.Position != new Vector2I(6, 5))
            throw new Exception("Enemy attacked immediately on player approach");
        game.EndTurn();
        if (game.PlayerState.Health >= health)
            throw new Exception("Adjacent enemy failed to attack on next action");
        game.PlayerState.Position = new Vector2I(5, 5);
        melee.Position = new Vector2I(7, 5);
        health = game.PlayerState.Health;
        game.EnemyAi.ActEnemy(melee);
        if (game.PlayerState.Health != health || GameRules.Dist(game.PlayerState.Position, melee.Position) != 1)
            throw new Exception("Enemy moved and attacked in the same action");
        for (int hero = 1; hero <= 2; hero++)
        {
            game.PlayerState.ClassIndex = hero;
            game.Start(90);
            game.DungeonState.Enemies.Clear();
            var delta = GameRules.Directions.First(d => game.DungeonState.Walk(game.PlayerState.Position + d));
            var victim = new Enemy(game.PlayerState.Position + delta, 'r', 1)
            {
                Health = 100
            };
            game.DungeonState.Enemies.Add(victim);
            int beforeTurn = game.RunState.Turn, beforeEnergy = game.PlayerState.Energy;
            var origin = game.PlayerState.Position;
            game.PlayerActions.Move(delta);
            game.CombatService.ResolveHeroAttack(victim);
            if (victim.Health != 100 || game.RunState.Turn != beforeTurn || game.PlayerState.Energy != beforeEnergy || game.PlayerState.Position != origin)
                throw new Exception("Ranged class performed a melee attack");
            game.InventoryState.Equipped[0] = null;
            game.RandomGenerator = new RegressionSuite.FixedRandom(20);
            game.PlayerActions.Move(delta);
            if (victim.Health != 96 || game.RunState.Turn != beforeTurn + 1 || game.HeroCombatStats.CanShoot || !game.HeroCombatStats.CanMelee)
                throw new Exception("Unarmed ranged class could not attack with 1d2");
            game.InventoryState.Equipped[0] = game.InventoryState.Backpack[0];
            int remaining = victim.Health;
            beforeTurn = game.RunState.Turn;
            game.PlayerActions.Move(delta);
            if (victim.Health != remaining || game.RunState.Turn != beforeTurn || game.HeroCombatStats.CanMelee)
                throw new Exception("Equipping ranged weapon did not disable melee");
        }

        GD.Print("AI AUDIT: patrol, visual detection, last-seen search, wall routing, occupancy, boss activation and confinement passed; 600 generated-map turns.");
    }
}
