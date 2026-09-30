using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
namespace Abyss.Tests;
internal sealed partial class RegressionSuite
{
    internal void TestExits()
    {
        var g = new GameSession(new TestHost(), new TestCanvas(), new TestSettings());
        var dungeon = g.DungeonState;
        void Verify()
        {
            if (dungeon.Tiles.Cast<char>().Count(c => c == '>') != 1 || dungeon.Tiles[dungeon.Stairs.X, dungeon.Stairs.Y] != '>') throw new Exception("Missing or duplicate exit.");
            var visited = new HashSet<Vector2I> { g.PlayerState.Position };
            var queue = new Queue<Vector2I>(); queue.Enqueue(g.PlayerState.Position);
            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                foreach (var direction in GameRules.Directions)
                    if (dungeon.Walk(p + direction) && visited.Add(p + direction)) queue.Enqueue(p + direction);
            }
            for (int y = 1; y < GameRules.Height - 1; y++)
                for (int x = 1; x < GameRules.Width - 1; x++)
                    if (dungeon.Walk(new(x,y)) && !visited.Contains(new(x,y))) throw new Exception("Room without a connected exit.");
            if (!visited.Contains(dungeon.Stairs)) throw new Exception("Unreachable stairs.");
        }
        for (int seed = 0; seed < 120; seed++)
            foreach (int floor in new[] { 1, 6, 11, 16, 21, 101 })
            {
                g.Start(seed); dungeon.Floor = floor; g.DungeonGenerator.Generate(false);
                g.FloorEventGenerator.Generate(FloorModifier.Infestation, false);
                Verify();
                dungeon.Visible[dungeon.Stairs.X, dungeon.Stairs.Y] = true;
                dungeon.Explored[dungeon.Stairs.X, dungeon.Stairs.Y] = true;
                if (EnvironmentAppearance.Sample(dungeon, dungeon.Stairs, .2).Glyph != '>') throw new Exception("Infestation hides the exit glyph.");
            }
        foreach (var modifier in Enum.GetValues<FloorModifier>())
        {
            g.Start(62); g.FloorEventGenerator.Generate(modifier, false); Verify();
        }
        g.DungeonGenerator.Generate(true); Verify();
        g.Start(1); dungeon.Enemies.Clear(); dungeon.Items.Clear();
        for (int y = 0; y < GameRules.Height; y++)
            for (int x = 0; x < GameRules.Width; x++) dungeon.Tiles[x,y] = '#';
        g.PlayerState.Position = new(2,2); dungeon.Tiles[2,2] = '.';
        dungeon.Stairs = new(50,20); dungeon.Tiles[20,10] = '.';
        DungeonConnectivity.EnsureExit(dungeon, g.PlayerState.Position); Verify();
        dungeon.Explored[50,20] = false;
        if (ExitAppearance.ShowMarker(dungeon, dungeon.Stairs, 0, 'g')) throw new Exception("Undiscovered stairs exposed through fog.");
        dungeon.Explored[50,20] = true;
        foreach (char actor in new[] { 'g', 'B', '@', '*' })
            if (!ExitAppearance.ShowMarker(dungeon, dungeon.Stairs, 0, actor) || ExitAppearance.ShowMarker(dungeon, dungeon.Stairs, .6, actor)) throw new Exception("Occupied exit lost its alternating marker.");
        GD.Print("EXIT AUDIT: 720 infested floors, all modifiers, merchant, connected chambers, repair, fog and occupied exit markers passed.");
    }
}
