using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Abyss.Application;
internal sealed class DungeonGenerator
{
    private readonly FloorEventGenerator floorEvents;
    private readonly InventoryState inventoryState;
    private readonly EnvironmentGenerator environmentGenerator;
    private readonly DungeonState dungeonState;
    private readonly ExpeditionJournal expeditionJournal;
    private readonly LootService lootService;
    private readonly MenuState menuState;
    private readonly MerchantState merchantState;
    private readonly PlayerState playerState;
    private readonly VisualEffects visualEffects;
    private readonly RandomStream random;
    internal DungeonGenerator(DungeonState dungeonState, ExpeditionJournal expeditionJournal, LootService lootService, MenuState menuState, MerchantState merchantState, PlayerState playerState, VisualEffects visualEffects, RandomStream random, InventoryState inventoryState, EnvironmentGenerator environmentGenerator, FloorEventGenerator floorEvents)
    {
        this.dungeonState = dungeonState;
        this.inventoryState = inventoryState;
        this.floorEvents = floorEvents;
        this.environmentGenerator = environmentGenerator;
        this.expeditionJournal = expeditionJournal;
        this.lootService = lootService;
        this.menuState = menuState;
        this.merchantState = merchantState;
        this.playerState = playerState;
        this.visualEffects = visualEffects;
        this.random = random;
    }

    internal bool MerchantEligible => dungeonState.Floor > 1 && !GameRules.IsBossFloor(dungeonState.Floor);

    internal void GenerateMerchantRoom()
    {
        dungeonState.IsMerchantFloor = true;
        dungeonState.Modifier = FloorModifier.None;
        dungeonState.Enemies.Clear();
        dungeonState.Items.Clear();
        visualEffects.ResetEffects();
        merchantState.MerchantStock.Clear();
        menuState.PendingTrade = null;
        for (int x = 0; x < GameRules.Width; x++)
            for (int y = 0; y < GameRules.Height; y++)
                dungeonState.Tiles[x, y] = '#';
        Array.Clear(dungeonState.Explored);
        Array.Clear(dungeonState.Visible);
        dungeonState.StairsRoom = new Rect2I(24, 8, 15, 10);
        for (int x = dungeonState.StairsRoom.Position.X; x < dungeonState.StairsRoom.End.X; x++)
            for (int y = dungeonState.StairsRoom.Position.Y; y < dungeonState.StairsRoom.End.Y; y++)
                dungeonState.Tiles[x, y] = '.';
        playerState.Position = new Vector2I(31, 16);
        dungeonState.MerchantPosition = new Vector2I(28, 11);
        dungeonState.Stairs = new Vector2I(36, 11);
        dungeonState.Tiles[dungeonState.Stairs.X, dungeonState.Stairs.Y] = '>';
        for (int i = 0; i < 6; i++)
            merchantState.MerchantStock.Add(new Offer { Gear = lootService.CreateEquipment(dungeonState.Floor) });
        merchantState.MerchantStock.Add(new Offer { Potion = 0, Quantity = random.Generator.Next(2, 5) });
        merchantState.MerchantStock.Add(new Offer { Potion = 1, Quantity = random.Generator.Next(1, 4) });
        menuState.MerchantQuote = random.Generator.Next(4);
        merchantState.MerchantStock.Add(new Offer { Potion = 2, Quantity = 4 });
        environmentGenerator.Generate();
        DungeonConnectivity.EnsureExit(dungeonState, playerState.Position);
        Reveal();
        expeditionJournal.Say("Uma luz acolhedora. Voce encontrou o mercador.", "A welcoming light. You found the merchant.");
    }

    internal void Generate(bool? merchantOverride = null)
    {
        visualEffects.ResetEffects();
        dungeonState.Enemies.Clear();
        dungeonState.Items.Clear();
        Array.Clear(dungeonState.Explored);
        Array.Clear(dungeonState.Visible);
        for (int x = 0; x < GameRules.Width; x++)
            for (int y = 0; y < GameRules.Height; y++)
                dungeonState.Tiles[x, y] = '#';
        dungeonState.IsMerchantFloor = false;
        dungeonState.Modifier = FloorModifier.None;
        merchantState.MerchantStock.Clear();
        menuState.PendingTrade = null;
        if (merchantOverride ?? (MerchantEligible && random.Generator.NextDouble() < .10))
        {
            GenerateMerchantRoom();
            return;
        }

        var rooms = new List<Rect2I>();
        for (int tries = 0; tries < 160 && rooms.Count < 10; tries++)
        {
            int rw = random.Generator.Next(6, 13), rh = random.Generator.Next(4, 8);
            var room = new Rect2I(random.Generator.Next(2, GameRules.Width - rw - 1), random.Generator.Next(2, GameRules.Height - rh - 1), rw, rh);
            if (rooms.Any(r => r.Grow(1).Intersects(room)))
                continue;
            for (int x = room.Position.X; x < room.End.X; x++)
                for (int y = room.Position.Y; y < room.End.Y; y++)
                    dungeonState.Tiles[x, y] = '.';
            if (rooms.Count > 0)
            {
                var a = rooms[^1].GetCenter();
                var b = room.GetCenter();
                while (a.X != b.X)
                {
                    dungeonState.Tiles[a.X, a.Y] = '.';
                    a.X += Math.Sign(b.X - a.X);
                }

                while (a.Y != b.Y)
                {
                    dungeonState.Tiles[a.X, a.Y] = '.';
                    a.Y += Math.Sign(b.Y - a.Y);
                }

                dungeonState.Tiles[a.X, a.Y] = '.';
            }

            rooms.Add(room);
        }

        dungeonState.StairsRoom = rooms[^1];
        playerState.Position = rooms[0].GetCenter();
        dungeonState.Stairs = dungeonState.StairsRoom.GetCenter();
        dungeonState.Tiles[dungeonState.Stairs.X, dungeonState.Stairs.Y] = '>';
        var free = new List<Vector2I>();
        for (int y = 1; y < GameRules.Height - 1; y++)
            for (int x = 1; x < GameRules.Width - 1; x++)
            {
                var p = new Vector2I(x, y);
                if (dungeonState.Walk(p) && p != dungeonState.Stairs && GameRules.Dist(p, playerState.Position) > 6)
                    free.Add(p);
            }

        Vector2I Take()
        {
            int i = random.Generator.Next(free.Count);
            var p = free[i];
            free.RemoveAt(i);
            return p;
        }

        int population = Math.Min(7 + (dungeonState.Floor - 1) / 2, 14) - (GameRules.IsBossFloor(dungeonState.Floor) ? 2 : 0);
        for (int i = 0; i < Math.Min(population, Math.Max(0, free.Count - 5)); i++)
        {
            char g = EnemyCatalog.Roster(dungeonState.Floor)[i % 3];
            dungeonState.Enemies.Add(new Enemy(Take(), g, dungeonState.Floor));
        }

        for (int i = 0; i < 2; i++)
            dungeonState.Items[Take()] = '$';
        if (random.Generator.NextDouble() < .75)
            dungeonState.Items[Take()] = '!';
        if (random.Generator.NextDouble() < .40)
            dungeonState.Items[Take()] = '*';
        if (free.Count > 0 && random.Generator.NextDouble() < GameRules.ChestChance(dungeonState.Floor))
            dungeonState.Items[Take()] = 'C';
        if (GameRules.IsBossFloor(dungeonState.Floor))
        {
            dungeonState.Enemies.Add(new Enemy(dungeonState.Stairs, 'B', dungeonState.Floor));
            expeditionJournal.Say($"Andar {dungeonState.Floor}: um Guardiao bloqueia a descida.", $"Floor {dungeonState.Floor}: a Warden blocks the descent.");
        }

        environmentGenerator.Generate();
        floorEvents.Generate();
        DungeonConnectivity.EnsureExit(dungeonState, playerState.Position);
        Reveal();
    }

    internal void Reveal()
    {
        Array.Clear(dungeonState.Visible);
        var lights = dungeonState.Environment.Fixtures.Where(f => f.Value == Fixture.WallTorch).Select(f => f.Key)
            .Concat(dungeonState.Environment.Fire.Keys).Where(p => dungeonState.Los(playerState.Position, p)).ToArray();
        for (int y = 0; y < GameRules.Height; y++)
            for (int x = 0; x < GameRules.Width; x++)
            {
                var p = new Vector2I(x, y);
                if ((dungeonState.Modifier == FloorModifier.Blackout
                    ? (p - playerState.Position).LengthSquared() <= 4
                    : (p - playerState.Position).LengthSquared() <= 100 &&
                    (inventoryState.HasLight || (p - playerState.Position).LengthSquared() <= 9 || lights.Any(light => GameRules.Dist(light, p) <= 2 && dungeonState.Los(light, p)))) &&
                    dungeonState.Los(playerState.Position, p))
                    dungeonState.Explored[x, y] = dungeonState.Visible[x, y] = true;
            }
    }
}
