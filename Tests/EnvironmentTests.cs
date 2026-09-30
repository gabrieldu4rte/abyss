using Godot;
using System;
using System.Linq;

namespace Abyss.Tests;
internal sealed partial class RegressionSuite
{
    internal void TestEnvironment()
    {
        var g = new GameSession(new TestHost(), new TestCanvas(), new TestSettings());
        var world = g.DungeonState.Environment;
        void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
        void EmptyRoom()
        {
            g.Start(731);
            g.DungeonState.Enemies.Clear(); g.DungeonState.Items.Clear(); world.Clear();
            g.DungeonState.Modifier = FloorModifier.None;
            for (int x = 1; x < GameRules.Width - 1; x++)
                for (int y = 1; y < GameRules.Height - 1; y++) g.DungeonState.Tiles[x, y] = '.';
            g.DungeonState.Stairs = new Vector2I(60, 25);
            g.PlayerState.Position = new Vector2I(20, 13);
            g.DungeonGenerator.Reveal();
        }
        int dryFloors = 0, trapFreeFloors = 0, barrelFreeFloors = 0;
        for (int seed = 0; seed < 20; seed++)
            for (int floor = 1; floor <= 21; floor++)
            {
                g.Start(seed); g.DungeonState.Floor = floor; g.DungeonGenerator.Generate(false);
                Assert((int)world.Biome == ((floor - 1) / 5) % 4, "Biome cycle changed inside five floors.");
                Assert(world.Fixtures.Count(f => f.Value == Fixture.WallTorch) >= 2, "Map lacks collectible torches.");
                foreach (var fixture in world.Fixtures)
                    Assert(g.DungeonState.Walk(fixture.Key) && fixture.Key != g.DungeonState.Stairs && fixture.Key != g.PlayerState.Position && !g.DungeonState.Items.ContainsKey(fixture.Key) && g.DungeonState.At(fixture.Key) == null, "Environment overlaps reserved content.");
                int water = world.Details.Count(d => d.Value == '~');
                int barrels = world.Fixtures.Count(f => f.Value == Fixture.OilBarrel);
                int traps = world.Fixtures.Count(f => TrapRules.IsTrap(f.Value));
                Assert(water <= (world.Biome == Biome.Cistern ? 26 : 13) && barrels <= 2 && traps <= 2, "Environmental density exceeds sparse generation limits.");
                if (water == 0) dryFloors++;
                if (barrels == 0) barrelFreeFloors++;
                if (traps == 0) trapFreeFloors++;
                Assert(world.Fixtures.Values.Where(TrapRules.IsTrap).All(f => f == TrapRules.ForBiome(world.Biome)), "Trap does not match its biome.");
                
                g.EnvironmentGenerator.Generate();
                var first = string.Join(";", world.Details) + string.Join(";", world.Fixtures);
                g.EnvironmentGenerator.Generate();
                Assert(first == string.Join(";", world.Details) + string.Join(";", world.Fixtures), "Environment generation is not deterministic.");
            }
        Assert(dryFloors > 150 && barrelFreeFloors > 120 && trapFreeFloors > 180, "Environmental features appear too frequently across floors.");
        g.DungeonGenerator.Generate(true);
        Assert(world.Oil.Count == 0 && world.Fire.Count == 0 && world.Fixtures.Values.All(f => f == Fixture.WallTorch), "Merchant room contains hazards.");
        EmptyRoom();
        Assert(g.InventoryState.HasLight && g.InventoryState.TorchFuel == 100 && g.InventoryState.SpareTorches == 0, "Starter torch missing.");
        g.RunState.Screen = "pause";
        g.Tick(200);
        Assert(g.InventoryState.TorchFuel == 100 && g.RunState.Turn == 0, "Real time consumed torch fuel.");
        g.RunState.Screen = "game";
        for (int i = 0; i < 99; i++) g.EndTurn();
        Assert(g.InventoryState.HasLight && g.InventoryState.TorchFuel == 1, "Torch did not last 99 turns.");
        g.EndTurn();
        Assert(!g.InventoryState.HasLight && g.InventoryState.TorchFuel == 0, "Torch did not expire at 100 turns.");
        Assert(g.DungeonState.Visible[23, 13] && !g.DungeonState.Visible[24, 13], "Unlit sight radius is not three.");
        g.InventoryState.SpareTorches = 2;
        g.PlayerActions.ToggleTorch();
        Assert(g.InventoryState.HasLight && g.InventoryState.TorchFuel == 99 && g.InventoryState.SpareTorches == 1 && g.DungeonState.Visible[30, 13], "Relighting failed.");
        g.PlayerActions.ToggleTorch(); int fuel = g.InventoryState.TorchFuel;
        g.EndTurn(); Assert(g.InventoryState.TorchFuel == fuel && !g.InventoryState.HasLight, "Stowed torch consumed fuel.");
        EmptyRoom();
        var origin = g.PlayerState.Position;
        var right = origin + Vector2I.Right;
        world.Fixtures[right] = Fixture.WallTorch;
        g.PlayerActions.Move(Vector2I.Right);
        Assert(g.PlayerState.Position == origin && g.DungeonState.Items[right] == 't', "Struck torch did not fall.");
        g.PlayerActions.Move(Vector2I.Right);
        Assert(g.InventoryState.SpareTorches == 1 && !g.DungeonState.Items.ContainsKey(right), "Fallen torch not collected exactly once.");
        EmptyRoom();
        g.PlayerActions.BeginTorchThrow();
        Assert(g.RunState.Screen == "game", "Throw started outside inventory.");
        g.GameInput.HandleKey(Key.I); g.MenuState.InventoryIndex = 3;
        g.GameInput.HandleKey(Key.T);
        Assert(g.RunState.Screen == "torch_aim", "Inventory throw did not enter directional aim.");
        g.GameInput.HandleKey(Key.Escape);
        Assert(g.RunState.Screen == "pause" && g.RunState.Turn == 0 && g.InventoryState.TorchFuel == 100, "Cancelling throw spent resources.");
        g.GameInput.HandleKey(Key.T);
        g.DungeonState.Tiles[21, 13] = '#';
        g.GameInput.HandleKey(Key.Right);
        Assert(g.RunState.Screen == "torch_aim" && g.RunState.Turn == 0 && g.InventoryState.HasLight, "Blocked throw consumed torch.");
        g.DungeonState.Tiles[21, 13] = '.';
        world.Fixtures[new Vector2I(23, 13)] = Fixture.OilBarrel;
        g.GameInput.HandleKey(Key.Right);
        Assert(g.RunState.Screen == "game" && !g.InventoryState.HasLight && world.Fire.Count >= 5 && g.RunState.Turn == 1, "Thrown torch did not ignite spilled barrel oil.");
        Assert(g.VisualEffects.Actions.Animations.Last().Path.Last() == new Vector2I(23, 13), "Torch passed through barrel.");
        EmptyRoom();
        world.Fixtures[right] = Fixture.OilBarrel;
        g.PlayerActions.Move(Vector2I.Right);
        Assert(world.Oil.Count == 5 && !world.Fixtures.ContainsKey(right), "Barrel did not spill adjacent oil.");
        var enemy = new Enemy(origin + Vector2I.Right * 2, 'g', 1) { Health = 30, MaxHealth = 30 };
        g.DungeonState.Enemies.Add(enemy);
        world.Details[origin + Vector2I.Right + Vector2I.Down] = '~';
        g.EnvironmentService.Ignite(right);
        Assert(!world.Fire.ContainsKey(origin + Vector2I.Right + Vector2I.Down), "Fire spread into water.");
        int health = g.PlayerState.Health;
        g.EnvironmentService.Tick();
        Assert(g.PlayerState.Health == health - 3 && enemy.Health == 27, "Area fire did not hit both actors.");
        for (int i = 0; i < 3; i++) g.EnvironmentService.Tick();
        Assert(world.Fire.Count == 0 && enemy.Health == 18, "Fire damage/duration is not four turns.");
        EmptyRoom();
        world.Fixtures[right] = Fixture.PoisonTrap;
        health = g.PlayerState.Health;
        g.PlayerActions.Move(Vector2I.Right);
        Assert(g.PlayerState.Health == health - 2 && world.HeroPoisonTurns == 2, "Poison trap failed on entry.");
        g.PlayerActions.Move(Vector2I.Left); g.EndTurn();
        Assert(g.PlayerState.Health == health - 6 && world.HeroPoisonTurns == 0 && world.Fixtures[right] == Fixture.SpentTrap, "Poison duration or spent trap failed.");
        g.PlayerActions.Move(Vector2I.Right);
        Assert(g.PlayerState.Health == health - 6, "Spent trap triggered twice.");
        world.HeroPoisonTurns = 2; g.DungeonGenerator.Generate(false);
        Assert(world.HeroPoisonTurns == 2, "Changing floors cured poison.");
        EmptyRoom();
        enemy = new Enemy(right, 'r', 1); g.DungeonState.Enemies.Add(enemy);
        world.Fixtures[right] = Fixture.PoisonTrap;
        int enemyHp = enemy.Health; g.EnvironmentService.Tick();
        Assert(enemy.Health == enemyHp - 2, "Enemy did not trigger poison trap.");
        EmptyRoom();
        g.PlayerState.Health = 1; world.Fire[origin] = 4;
        g.EnvironmentService.Tick();
        Assert(g.PlayerState.Health == 0 && g.RunState.Screen == "dead", "Environmental damage did not end expedition.");
        EmptyRoom();
        g.DungeonGenerator.Generate(true); g.PlayerState.Gold = 100;
        g.MenuState.PendingTrade = g.MerchantState.MerchantStock.Single(o => o.Gear == null && o.Potion == 2);
        g.MenuState.ShopSelling = false; g.MerchantService.CompleteTrade();
        Assert(g.InventoryState.SpareTorches == 1 && g.PlayerState.Gold == 92, "Torch purchase failed.");
        g.MenuState.ShopSelling = true;
        g.MenuState.PendingTrade = g.MerchantService.ShopOffers().Single(o => o.Gear == null && o.Potion == 2);
        g.MerchantService.CompleteTrade();
        Assert(g.InventoryState.SpareTorches == 0 && g.PlayerState.Gold == 96 && g.InventoryState.HasLight, "Torch sale touched equipped light or currency.");
        EmptyRoom(); g.RandomGenerator = new Random(177);
        for (int i = 0; i < 100; i++) g.LootService.OpenChest();
        Assert(g.InventoryState.SpareTorches > 0, "Chests never yield torches.");
        EmptyRoom();
        world.Details[origin + Vector2I.Right * 5] = '~';
        g.GameInput.HandleKey(Key.I); g.MenuState.InventoryIndex = 3; g.GameInput.HandleKey(Key.T); g.GameInput.HandleKey(Key.Right);
        Assert(world.Fire.Count == 0 && !g.InventoryState.HasLight, "Water did not extinguish a thrown torch.");
        EmptyRoom();
        world.Fixtures[origin + Vector2I.Right * 5] = Fixture.WallTorch;
        g.InventoryState.TorchEquipped = false; g.DungeonGenerator.Reveal();
        Assert(g.DungeonState.Visible[25, 13], "Nearby fixed torch casts no light.");
        g.DungeonState.Tiles[22, 13] = '#'; g.DungeonGenerator.Reveal();
        Assert(!g.DungeonState.Visible[25, 13], "Light leaks through walls.");
        EmptyRoom();
        world.Fixtures[right] = Fixture.OilBarrel;
        world.Fixtures[right + Vector2I.Right * 2] = Fixture.OilBarrel;
        g.EnvironmentService.Ignite(right);
        Assert(world.Fire.Count > 5 && !world.Fixtures.Values.Contains(Fixture.OilBarrel), "Fire failed to chain through nearby barrels.");
        EmptyRoom();
        enemy = new Enemy(right, 'r', 1) { Health = 1 }; g.DungeonState.Enemies.Add(enemy);
        world.Fire[right] = 4;
        g.EnvironmentService.Tick(); g.EnvironmentService.Tick();
        Assert(g.PlayerState.Kills == 1 && !g.DungeonState.Enemies.Contains(enemy), "Environmental death gave duplicate rewards.");
        EmptyRoom();
        world.Details[right] = '~';
        Assert(Enumerable.Range(0, 8).Select(i => EnvironmentAppearance.Sample(g.DungeonState, right, i * .2)).Distinct().Count() > 1, "Water does not animate.");
        GD.Print("ENVIRONMENT AUDIT: 420 floors, biome cycles, safe merchants, deterministic details, 100-turn fuel, vision, inventory throwing, collection, fire, poison, death, chest drops and torch trading passed.");
    }
}
