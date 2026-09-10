using System.Numerics;
using Raid.Battle.Commands;
using Raid.Battle.Entities;
using Raid.Battle.Events;
using Raid.Battle.World;
using Raid.Contracts.Common;

namespace Raid.Battle.Tests;

public sealed class SkillTargetPersistenceTests
{
    [Fact]
    public void EntitySkill_PreservesTargetOnActionAndEvents()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.instant_strike")!;
        var dummy = world.Entities.Dummies().Single();
        var target = new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id);

        world.Commands.Enqueue(new UseSkillCommand(player.Id, skill, target));
        world.Loop.Tick();

        Assert.Equal(target, player.Actions.CurrentAction?.Target);

        var events = world.Events.Drain();
        Assert.Contains(events, battleEvent => battleEvent is ActionStartedEvent started
            && started.Target == target);
        Assert.Contains(events, battleEvent => battleEvent is ActionPhaseChangedEvent changed
            && changed.Target == target);
    }

    [Fact]
    public void PointSkill_PreservesTargetOnActionAndEvents()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.meteor_strike")!;
        var dummy = world.Entities.Dummies().Single();
        var target = new SkillTarget(SkillTargetingMode.Point, Position: dummy.Position);

        world.Commands.Enqueue(new UseSkillCommand(player.Id, skill, target));
        world.Loop.Tick();

        Assert.Equal(target, player.Actions.CurrentAction?.Target);
        Assert.Null(player.Actions.CurrentAction?.TargetId);

        var events = world.Events.Drain();
        Assert.Contains(events, battleEvent => battleEvent is ActionStartedEvent started
            && started.Target == target);
        Assert.Contains(events, battleEvent => battleEvent is ActionPhaseChangedEvent changed
            && changed.Target == target);
    }

    [Fact]
    public void DirectionSkill_PreservesTargetOnActionAndEvents()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.arrow_strike")!;
        var target = new SkillTarget(SkillTargetingMode.Direction, Direction: -Vector2.UnitX);

        world.Commands.Enqueue(new UseSkillCommand(player.Id, skill, target));
        world.Loop.Tick();

        Assert.Equal(target, player.Actions.CurrentAction?.Target);
        Assert.Null(player.Actions.CurrentAction?.TargetId);

        var events = world.Events.Drain();
        Assert.Contains(events, battleEvent => battleEvent is ActionStartedEvent started
            && started.Target == target);
        Assert.Contains(events, battleEvent => battleEvent is ActionPhaseChangedEvent changed
            && changed.Target == target);
    }

    [Fact]
    public void NoneSkill_PreservesEmptyTarget()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.hold_guard")!;
        var target = new SkillTarget(SkillTargetingMode.None);

        world.Commands.Enqueue(new UseSkillCommand(player.Id, skill, target));
        world.Loop.Tick();

        Assert.Equal(target, player.Actions.CurrentAction?.Target);
        Assert.Null(player.Actions.CurrentAction?.TargetId);
    }

    private static (BattleWorld World, PlayerEntity Player) CreatePracticeWithPlayer()
    {
        var setup = BattleWorldFactory.Create(RaidMode.Practice, new WorldSettings());
        var player = PracticeSpawn.Player(setup.World, "player-1", participantSlot: 0);
        return (setup.World, player);
    }
}
