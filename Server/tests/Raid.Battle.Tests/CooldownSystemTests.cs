using System.Numerics;
using Raid.Battle.Commands;
using Raid.Battle.Definitions;
using Raid.Battle.Entities;
using Raid.Battle.Events;
using Raid.Battle.World;
using Raid.Contracts.Common;

namespace Raid.Battle.Tests;

public sealed class CooldownSystemTests
{
    [Fact]
    public void Activation_StartsCooldown_AndEmitsEvent()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = TestClassDefinition.InstantStrike;
        var dummy = world.Entities.Dummies().Single();

        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id)));
        world.Loop.Tick();

        Assert.True(player.SkillCooldownRemainingSeconds.ContainsKey(skill.SkillId));

        var events = world.Events.Drain();
        Assert.Contains(events, battleEvent => battleEvent is CooldownStartedEvent started
            && started.SkillId == skill.SkillId
            && started.DurationSeconds == skill.CooldownMilliseconds / 1000f);
    }

    [Fact]
    public void UseSkill_FailsWhileOnCooldown()
    {
        var (world, player) = CreatePracticeWithPlayer(new WorldSettings
        {
            FixedDeltaMilliseconds = 100
        });
        var skill = TestClassDefinition.InstantStrike;
        var dummy = world.Entities.Dummies().Single();
        var target = new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id);

        world.Commands.Enqueue(new UseSkillCommand(player.Id, skill, target));
        world.Loop.Tick();
        world.Events.Drain();

        while (player.Actions.IsBusy)
        {
            world.Loop.Tick();
        }

        var result = new UseSkillCommand(player.Id, skill, target)
            .Execute(new CommandContext(world));

        Assert.False(result.Succeeded);
        Assert.Equal(UseSkillFailureReason.SkillOnCooldown, result.UseSkillFailure);
    }

    [Fact]
    public void CastingCancel_DoesNotStartCooldown()
    {
        var (world, player) = CreatePracticeWithPlayer(new WorldSettings
        {
            FixedDeltaMilliseconds = 100
        });
        var skill = TestClassDefinition.ChargedStrike;
        var dummy = world.Entities.Dummies().Single();
        var target = new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id);

        world.Commands.Enqueue(new UseSkillCommand(player.Id, skill, target));
        world.Loop.Tick();

        world.Commands.Enqueue(new MoveCommand(player.Id, new Vector2(1f, 0f)));
        world.Loop.Tick();

        Assert.False(player.SkillCooldownRemainingSeconds.ContainsKey(skill.SkillId));

        var result = new UseSkillCommand(player.Id, skill, target)
            .Execute(new CommandContext(world));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void IgnoreCooldowns_AllowsImmediateReuse()
    {
        var settings = new WorldSettings { FixedDeltaMilliseconds = 50 };
        settings.Practice.IgnoreCooldowns = true;
        var setup = BattleWorldFactory.Create(RaidMode.Practice, settings);
        var world = setup.World;
        var player = BattleWorldFactory.SpawnPlayer(world, "player-1", participantSlot: 0);
        var skill = TestClassDefinition.InstantStrike;
        var dummy = world.Entities.Dummies().Single();
        var target = new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id);

        world.Commands.Enqueue(new UseSkillCommand(player.Id, skill, target));
        world.Loop.Tick();
        world.Events.Drain();

        while (player.Actions.IsBusy)
        {
            world.Loop.Tick();
        }

        var result = new UseSkillCommand(player.Id, skill, target)
            .Execute(new CommandContext(world));

        Assert.True(result.Succeeded);
        Assert.False(player.SkillCooldownRemainingSeconds.ContainsKey(skill.SkillId));
    }

    [Fact]
    public void Cooldown_Expires_AndEmitsReadyEvent()
    {
        var (world, player) = CreatePracticeWithPlayer(new WorldSettings
        {
            FixedDeltaMilliseconds = 1000
        });
        var skill = TestClassDefinition.InstantStrike;
        var dummy = world.Entities.Dummies().Single();

        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id)));
        world.Loop.Tick();
        world.Events.Drain();

        for (var i = 0; i < skill.CooldownMilliseconds / 1000; i++)
        {
            world.Loop.Tick();
        }

        Assert.False(player.SkillCooldownRemainingSeconds.ContainsKey(skill.SkillId));

        var events = world.Events.Drain();
        Assert.Contains(events, battleEvent => battleEvent is CooldownReadyEvent ready
            && ready.SkillId == skill.SkillId);
    }

    private static (BattleWorld World, PlayerEntity Player) CreatePracticeWithPlayer(
        WorldSettings? settings = null)
    {
        var setup = BattleWorldFactory.Create(RaidMode.Practice, settings);
        var player = BattleWorldFactory.SpawnPlayer(setup.World, "player-1", participantSlot: 0);
        return (setup.World, player);
    }
}
