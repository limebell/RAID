using System.Numerics;
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
        var world = new BattleWorld(new WorldSettings
        {
            FixedDeltaMilliseconds = 100
        });

        var player = world.CreatePlayer(
            userId: "user-1",
            participantSlot: 0,
            playerClass: TestClassDefinition.Create(),
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
        var world = new BattleWorld(new WorldSettings
        {
            FixedDeltaMilliseconds = 100
        });

        var player = world.CreatePlayer(
            userId: "user-1",
            participantSlot: 0,
            playerClass: TestClassDefinition.Create(),
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

        world.Loop.Tick();
        Assert.Equal(stoppedPosition, player.Position);
        Assert.Null(player.ActiveMovement);
    }

    [Fact]
    public void MoveCommand_UpdatesFacingDirection_FromMovementVector()
    {
        var world = new BattleWorld(new WorldSettings
        {
            FixedDeltaMilliseconds = 100
        });

        var player = world.CreatePlayer(
            userId: "user-1",
            participantSlot: 0,
            playerClass: TestClassDefinition.Create(),
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
        var world = new BattleWorld(new WorldSettings
        {
            FixedDeltaMilliseconds = 100
        });

        var player = world.CreatePlayer(
            userId: "user-1",
            participantSlot: 0,
            playerClass: TestClassDefinition.Create(),
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
        var world = new BattleWorld(new WorldSettings
        {
            FixedDeltaMilliseconds = 100
        });

        var player = world.CreatePlayer(
            userId: "user-1",
            participantSlot: 0,
            playerClass: TestClassDefinition.Create(),
            position: Vector2.Zero,
            facingDirection: Vector2.UnitX,
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
        var world = new BattleWorld();
        var player = world.CreatePlayer(
            "user-1",
            0,
            TestClassDefinition.Create(),
            new Vector2(1f, 2f),
            Vector2.UnitX);

        var events = world.Events.Drain();
        Assert.Contains(events, battleEvent => battleEvent is EntitySpawnedEvent spawned
            && spawned.Entity.EntityId == player.Id
            && spawned.Entity.Kind == EntityKind.Player
            && spawned.Entity.DefinitionId == TestClassDefinition.ClassId
            && spawned.Entity.Position == new Vector2(1f, 2f));
    }

    [Fact]
    public void BattleWorld_CanRegisterAndRemoveEntities()
    {
        var world = new BattleWorld();
        var player = world.CreatePlayer(
            "user-2",
            1,
            TestClassDefinition.Create(),
            Vector2.Zero,
            Vector2.UnitX);

        Assert.Same(player, world.Entities.Find<PlayerEntity>(player.Id));
        Assert.True(world.Remove(player.Id));
        Assert.Null(world.Entities.Find(player.Id));
    }
}
