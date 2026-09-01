using System.Numerics;
using Raid.Battle.Commands;
using Raid.Battle.Definitions;
using Raid.Battle.Entities;
using Raid.Battle.Events;
using Raid.Battle.Movement;
using Raid.Battle.World;
using Raid.Contracts.Common;

namespace Raid.Battle.Tests;

public sealed class PracticeModeTests
{
    [Fact]
    public void InstantSkill_DamagesDummy()
    {
        var (world, player) = CreatePracticeWithPlayer(new WorldSettings
        {
            FixedDeltaMilliseconds = 50
        });

        var skill = TestClassDefinition.InstantStrike;
        var dummy = world.Entities.Dummies().Single();
        var healthBefore = dummy.CurrentHealth;
        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(
                Mode: SkillTargetingMode.Entity,
                EntityId: dummy.Id)));

        for (var i = 0; i < 2; i++)
        {
            world.Loop.Tick();
        }

        Assert.Equal(healthBefore - skill.Damage, dummy.CurrentHealth);

        var events = world.Events.Drain();
        Assert.Contains(events, e => e is DamageAppliedEvent damage
            && damage.Target.EntityId == dummy.Id
            && damage.Amount == skill.Damage);
    }

    [Fact]
    public void CastSkill_AppliesDamage_AfterCastAndWindupComplete()
    {
        var (world, player) = CreatePracticeWithPlayer(new WorldSettings
        {
            FixedDeltaMilliseconds = 500
        });

        var skill = TestClassDefinition.MeteorStrike;
        var dummy = world.Entities.Dummies().Single();
        var healthBefore = dummy.CurrentHealth;
        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            PointTargetAt(dummy)));

        world.Loop.Tick();
        Assert.Equal(healthBefore, dummy.CurrentHealth);
        Assert.True(player.Actions.IsCasting);

        world.Loop.Tick();
        Assert.Equal(healthBefore, dummy.CurrentHealth);
        Assert.Equal(Raid.Battle.Actions.ActionPhaseKind.Windup, player.Actions.CurrentAction?.CurrentPhaseKind);

        world.Loop.Tick();
        Assert.Equal(healthBefore, dummy.CurrentHealth);
        Assert.False(player.Actions.IsCasting);
    }

    [Fact]
    public void Move_CancelsCasting()
    {
        var (world, player) = CreatePracticeWithPlayer(new WorldSettings
        {
            FixedDeltaMilliseconds = 100
        });

        var skill = TestClassDefinition.MeteorStrike;
        var dummy = world.Entities.Dummies().Single();
        var healthBefore = dummy.CurrentHealth;
        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            PointTargetAt(dummy)));
        world.Loop.Tick();

        world.Commands.Enqueue(new MoveCommand(player.Id, new Vector2(1f, 0f)));
        world.Loop.Tick();

        Assert.False(player.Actions.IsCasting);
        Assert.Equal(healthBefore, dummy.CurrentHealth);

        var events = world.Events.Drain();
        Assert.Contains(events, e => e is ActionEndedEvent ended
            && ended.Reason == Raid.Battle.Actions.ActionEndReason.CancelledByMove);
    }

    [Fact]
    public void Move_DoesNotCancelWindup()
    {
        var (world, player) = CreatePracticeWithPlayer(new WorldSettings
        {
            FixedDeltaMilliseconds = 500
        });

        var skill = TestClassDefinition.MeteorStrike;
        var dummy = world.Entities.Dummies().Single();
        var healthBefore = dummy.CurrentHealth;
        var startPosition = player.Position;
        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            PointTargetAt(dummy)));

        world.Loop.Tick();
        world.Loop.Tick();
        Assert.Equal(Raid.Battle.Actions.ActionPhaseKind.Windup, player.Actions.CurrentAction?.CurrentPhaseKind);

        world.Commands.Enqueue(new MoveCommand(player.Id, new Vector2(1f, 0f)));
        world.Loop.Tick();

        Assert.Equal(healthBefore, dummy.CurrentHealth);
        Assert.Equal(startPosition, player.Position);
        Assert.False(player.Actions.IsCasting);
    }

    [Fact]
    public void PositionSet_OverridesActiveMove()
    {
        var (world, player) = CreatePracticeWithPlayer();
        world.Commands.Enqueue(new MoveCommand(player.Id, new Vector2(10f, 0f)));
        world.Loop.Tick();

        world.Movement.SetPosition(new PositionSetRequest(
            player.Id,
            new Vector2(2f, 3f),
            PositionSetReason.Reset));

        Assert.Equal(new Vector2(2f, 3f), player.Position);
        Assert.Null(player.ActiveMovement);
    }

    [Fact]
    public void PracticeMode_UsesTestClassSkills()
    {
        var setup = BattleWorldFactory.Create(RaidMode.Practice);
        var player = BattleWorldFactory.SpawnPlayer(setup.World, "player-1", participantSlot: 0);

        Assert.Equal(RaidMode.Practice, setup.Mode);
        Assert.Equal(TestClassDefinition.ClassId, player.Class.ClassId);
        Assert.NotNull(player.FindSkill(TestClassDefinition.InstantStrike.SkillId));
        Assert.NotNull(player.FindSkill(TestClassDefinition.MeteorStrike.SkillId));
        Assert.NotNull(player.FindSkill(TestClassDefinition.ArrowStrike.SkillId));
    }

    [Fact]
    public void PracticeMode_CanSpawnMultiplePlayers()
    {
        var setup = BattleWorldFactory.Create(RaidMode.Practice);
        var first = BattleWorldFactory.SpawnPlayer(setup.World, "player-1", participantSlot: 0);
        var second = BattleWorldFactory.SpawnPlayer(setup.World, "player-2", participantSlot: 1);

        Assert.Equal(2, setup.World.Entities.Players().Count());
        Assert.NotEqual(first.Id, second.Id);
        Assert.NotEqual(first.Position, second.Position);
    }

    private static SkillTarget PointTargetAt(BattleEntity target) =>
        new(SkillTargetingMode.Point, Position: target.Position);

    private static (BattleWorld World, PlayerEntity Player) CreatePracticeWithPlayer(
        WorldSettings? settings = null)
    {
        var setup = BattleWorldFactory.Create(RaidMode.Practice, settings);
        var player = BattleWorldFactory.SpawnPlayer(setup.World, "player-1", participantSlot: 0);
        return (setup.World, player);
    }
}
