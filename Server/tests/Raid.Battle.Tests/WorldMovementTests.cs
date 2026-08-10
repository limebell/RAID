using System.Numerics;
using Raid.Battle.Commands;
using Raid.Battle.Definitions;
using Raid.Battle.Entities;
using Raid.Battle.Events;
using Raid.Battle.World;

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
            moveSpeed: 5f);

        world.Commands.Enqueue(new MoveCommand(player.Id, new Vector2(1f, 0f)));

        world.Loop.Tick();
        world.Loop.Tick();

        Assert.Equal(new Vector2(1f, 0f), player.Position);
        Assert.Null(player.ActiveMove);

        var events = world.Events.Drain();
        Assert.Contains(events, battleEvent => battleEvent is EntityMoveCompletedEvent completed
            && completed.EntityId == player.Id
            && completed.Position == new Vector2(1f, 0f));
    }

    [Fact]
    public void BattleWorld_CanRegisterAndRemoveEntities()
    {
        var world = new BattleWorld();
        var player = world.CreatePlayer(
            "user-2",
            1,
            TestClassDefinition.Create(),
            Vector2.Zero);

        Assert.Same(player, world.Entities.Find<PlayerEntity>(player.Id));
        Assert.True(world.Remove(player.Id));
        Assert.Null(world.Entities.Find(player.Id));
    }
}
