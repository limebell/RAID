using System.Numerics;
using Raid.Battle.Collision;
using Raid.Battle.Commands;
using Raid.Battle.Definitions;
using Raid.Battle.Entities;
using Raid.Battle.Events;
using Raid.Battle.Movement;
using Raid.Battle.World;
using Raid.Contracts.Common;

namespace Raid.Battle.Tests;

public sealed class WorldMovementTests
{
    [Fact]
    public void MoveCommand_UpdatesPlayerPosition_AndEmitsCompletionEvent()
    {
        var world = new BattleWorld(
            new WorldSettings
            {
                FixedDeltaMilliseconds = 100
            },
            MapDefinition.Unbounded,
            "test");

        var player = world.CreatePlayer(
            userId: "user-1",
            participantSlot: 0,
            playerClass: ClassDefinitionLoader.Load("test"),
            position: Vector2.Zero,
            facingDirection: Vector2.UnitX,
            moveSpeed: 5f,
            turnSpeedRadiansPerSecond: 100f);

        world.Commands.Enqueue(new MoveCommand(player.Id, new Vector2(1f, 0f)));

        world.Loop.Tick();
        world.Loop.Tick();

        Assert.Equal(new Vector2(1f, 0f), player.Position);
        Assert.Equal(Vector2.UnitX, player.FacingDirection);
        Assert.Null(player.ActiveMovement);

        var events = world.Events.Drain();
        Assert.Contains(events, battleEvent => battleEvent is EntityMoveCompletedEvent completed
            && completed.Entity.EntityId == player.Id
            && completed.Entity.Position == new Vector2(1f, 0f));
    }

    [Fact]
    public void StopMovingCommand_ClearsActiveMovement_AndStopsAtCurrentPosition()
    {
        var world = new BattleWorld(
            new WorldSettings
            {
                FixedDeltaMilliseconds = 100
            },
            MapDefinition.Unbounded,
            "test");

        var player = world.CreatePlayer(
            userId: "user-1",
            participantSlot: 0,
            playerClass: ClassDefinitionLoader.Load("test"),
            position: Vector2.Zero,
            facingDirection: Vector2.UnitX,
            moveSpeed: 5f,
            turnSpeedRadiansPerSecond: 100f);

        world.Commands.Enqueue(new MoveCommand(player.Id, new Vector2(10f, 0f)));
        world.Loop.Tick();

        Assert.NotNull(player.ActiveMovement);
        Assert.NotEqual(Vector2.Zero, player.Position);

        world.Commands.Enqueue(new StopMovingCommand(player.Id));
        world.Loop.Tick();

        var stoppedPosition = player.Position;
        Assert.Null(player.ActiveMovement);

        var events = world.Events.Drain();
        Assert.Contains(events, battleEvent => battleEvent is EntityMoveCompletedEvent completed
            && completed.Entity.EntityId == player.Id
            && completed.Entity.Position == stoppedPosition);

        world.Loop.Tick();
        Assert.Equal(stoppedPosition, player.Position);
        Assert.Null(player.ActiveMovement);
    }

    [Fact]
    public void MoveCommand_UpdatesFacingDirection_FromMovementVector()
    {
        var world = new BattleWorld(
            new WorldSettings
            {
                FixedDeltaMilliseconds = 100
            },
            MapDefinition.Unbounded,
            "test");

        var player = world.CreatePlayer(
            userId: "user-1",
            participantSlot: 0,
            playerClass: ClassDefinitionLoader.Load("test"),
            position: Vector2.Zero,
            facingDirection: Vector2.UnitX,
            moveSpeed: 5f,
            turnSpeedRadiansPerSecond: 100f);

        world.Commands.Enqueue(new MoveCommand(player.Id, new Vector2(0f, 2f)));

        world.Loop.Tick();

        Assert.True(Vector2.Distance(player.FacingDirection, Vector2.UnitY) < 0.001f);
    }

    [Fact]
    public void RotateThenMove_WaitsForFacingAlignment_BeforeMoving()
    {
        var world = new BattleWorld(
            new WorldSettings
            {
                FixedDeltaMilliseconds = 100
            },
            MapDefinition.Unbounded,
            "test");

        var player = world.CreatePlayer(
            userId: "user-1",
            participantSlot: 0,
            playerClass: ClassDefinitionLoader.Load("test"),
            position: Vector2.Zero,
            facingDirection: Vector2.UnitX,
            moveSpeed: 5f,
            turnSpeedRadiansPerSecond: 4f);

        world.Movement.SetIntent(new MovementIntent(
            player.Id,
            Destination: new Vector2(-2f, 0f),
            DesiredFacingDirection: -Vector2.UnitX,
            MoveSpeed: player.MoveSpeed,
            TurnSpeedRadiansPerSecond: player.TurnSpeedRadiansPerSecond,
            FacingPolicy: MovementFacingPolicy.RotateThenMove));

        world.Loop.Tick();

        Assert.Equal(Vector2.Zero, player.Position);
        Assert.NotEqual(Vector2.UnitX, player.FacingDirection);

        for (var i = 0; i < 8; i++)
        {
            world.Loop.Tick();
        }

        Assert.True(player.Position.X < 0f);
    }

    [Fact]
    public void TurnOnly_RotatesWithoutMoving_AndEmitsRotationEvent()
    {
        var world = new BattleWorld(
            new WorldSettings
            {
                FixedDeltaMilliseconds = 100
            },
            MapDefinition.Unbounded,
            "test");

        var player = world.CreatePlayer(
            userId: "user-1",
            participantSlot: 0,
            playerClass: ClassDefinitionLoader.Load("test"),
            position: Vector2.Zero,
            facingDirection: Vector2.UnitX,
            moveSpeed: ClassDefinitionLoader.Load("test").MoveSpeed,
            turnSpeedRadiansPerSecond: 100f);

        world.Movement.SetDirectionIntent(player.Id, Vector2.UnitY);

        world.Loop.Tick();

        Assert.Equal(Vector2.Zero, player.Position);
        Assert.True(Vector2.Distance(player.FacingDirection, Vector2.UnitY) < 0.001f);
        Assert.Null(player.ActiveMovement);

        var events = world.Events.Drain();
        Assert.Contains(events, battleEvent => battleEvent is EntityMovedEvent moved
            && moved.Entity.EntityId == player.Id
            && moved.Entity.Position == Vector2.Zero
            && Vector2.Distance(moved.Entity.FacingDirection, Vector2.UnitY) < 0.001f);
    }

    [Fact]
    public void CreatePlayer_EmitsEntitySpawnedEvent()
    {
        var playerClass = ClassDefinitionLoader.Load("test");
        var world = new BattleWorld(new WorldSettings(), MapDefinition.Unbounded, playerClass.ClassId);
        var player = world.CreatePlayer(
            "user-1",
            0,
            playerClass,
            new Vector2(1f, 2f),
            Vector2.UnitX,
            playerClass.MoveSpeed,
            playerClass.TurnSpeedRadiansPerSecond);

        var events = world.Events.Drain();
        Assert.Contains(events, battleEvent => battleEvent is EntitySpawnedEvent spawned
            && spawned.Entity.EntityId == player.Id
            && spawned.Entity.Kind == EntityKind.Player
            && spawned.Entity.DefinitionId == "test"
            && spawned.Entity.Position == new Vector2(1f, 2f));
    }

    [Fact]
    public void MoveCommand_Stops_WhenBlockedByEntity()
    {
        var world = new BattleWorld(
            new WorldSettings
            {
                FixedDeltaMilliseconds = 100
            },
            MapDefinition.Unbounded,
            "test");

        var player = world.CreatePlayer(
            userId: "user-1",
            participantSlot: 0,
            playerClass: ClassDefinitionLoader.Load("test"),
            position: Vector2.Zero,
            facingDirection: Vector2.UnitX,
            moveSpeed: 5f,
            turnSpeedRadiansPerSecond: 100f);

        world.CreateDummy(new Vector2(2f, 0f), EntityDefinitionLoader.Load("practice.dummy"));

        world.Commands.Enqueue(new MoveCommand(player.Id, new Vector2(10f, 0f)));

        for (var i = 0; i < 10; i++)
        {
            world.Loop.Tick();
        }

        Assert.Null(player.ActiveMovement);
        Assert.True(player.Position.X < 2f);
        Assert.True(
            Vector2.Distance(player.Position, new Vector2(2f, 0f))
            >= player.CollisionRadius + EntityDefinitionLoader.Load("practice.dummy").CollisionRadius - 0.01f);
    }

    [Fact]
    public void MoveCommand_AllowsLeavingOverlap_ButNotMovingCloser()
    {
        var world = new BattleWorld(
            new WorldSettings
            {
                FixedDeltaMilliseconds = 100
            },
            MapDefinition.Unbounded,
            "test");

        var player = world.CreatePlayer(
            userId: "user-1",
            participantSlot: 0,
            playerClass: ClassDefinitionLoader.Load("test"),
            position: new Vector2(0.2f, 0f),
            facingDirection: Vector2.UnitX,
            moveSpeed: 5f,
            turnSpeedRadiansPerSecond: 100f);

        world.CreateDummy(Vector2.Zero, EntityDefinitionLoader.Load("practice.dummy"));

        world.Commands.Enqueue(new MoveCommand(player.Id, Vector2.Zero));
        world.Loop.Tick();

        Assert.Equal(new Vector2(0.2f, 0f), player.Position);
        Assert.Null(player.ActiveMovement);

        world.Commands.Enqueue(new MoveCommand(player.Id, new Vector2(3f, 0f)));
        world.Loop.Tick();

        Assert.True(player.Position.X > 0.2f);
    }

    [Fact]
    public void MoveCommand_Stops_AtMapBounds()
    {
        var world = new BattleWorld(
            new WorldSettings
            {
                FixedDeltaMilliseconds = 100
            },
            new MapDefinition(
                "test-bounds",
                Bounds: new Aabb(new Vector2(-2f, -2f), new Vector2(2f, 2f)),
                Obstacles: []),
            "test");

        var player = world.CreatePlayer(
            userId: "user-1",
            participantSlot: 0,
            playerClass: ClassDefinitionLoader.Load("test"),
            position: Vector2.Zero,
            facingDirection: Vector2.UnitX,
            moveSpeed: 5f,
            turnSpeedRadiansPerSecond: 100f);

        world.Commands.Enqueue(new MoveCommand(player.Id, new Vector2(10f, 0f)));

        for (var i = 0; i < 10; i++)
        {
            world.Loop.Tick();
        }

        Assert.Null(player.ActiveMovement);
        Assert.True(player.Position.X + player.CollisionRadius <= 2f + 0.01f);
    }

    [Fact]
    public void MoveCommand_Stops_AtStaticObstacle()
    {
        var world = new BattleWorld(
            new WorldSettings
            {
                FixedDeltaMilliseconds = 100
            },
            new MapDefinition(
                "test-obstacle",
                Bounds: null,
                Obstacles: [new Aabb(new Vector2(1.5f, -1f), new Vector2(2.5f, 1f))]),
            "test");

        var player = world.CreatePlayer(
            userId: "user-1",
            participantSlot: 0,
            playerClass: ClassDefinitionLoader.Load("test"),
            position: Vector2.Zero,
            facingDirection: Vector2.UnitX,
            moveSpeed: 5f,
            turnSpeedRadiansPerSecond: 100f);

        world.Commands.Enqueue(new MoveCommand(player.Id, new Vector2(10f, 0f)));

        for (var i = 0; i < 10; i++)
        {
            world.Loop.Tick();
        }

        Assert.Null(player.ActiveMovement);
        Assert.True(player.Position.X < 1.5f);
    }

    [Fact]
    public void BattleWorld_CanRegisterAndRemoveEntities()
    {
        var playerClass = ClassDefinitionLoader.Load("test");
        var world = new BattleWorld(new WorldSettings(), MapDefinition.Unbounded, playerClass.ClassId);
        var player = world.CreatePlayer(
            "user-2",
            1,
            playerClass,
            Vector2.Zero,
            Vector2.UnitX,
            playerClass.MoveSpeed,
            playerClass.TurnSpeedRadiansPerSecond);

        Assert.Same(player, world.Entities.Find<PlayerEntity>(player.Id));
        Assert.True(world.Remove(player.Id));
        Assert.Null(world.Entities.Find(player.Id));
    }
}
