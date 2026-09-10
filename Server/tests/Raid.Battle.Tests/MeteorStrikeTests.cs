using System.Numerics;
using Raid.Battle.Commands;
using Raid.Battle.Entities;
using Raid.Battle.Events;
using Raid.Battle.Movement;
using Raid.Battle.World;
using Raid.Contracts.Common;

namespace Raid.Battle.Tests;

public sealed class MeteorStrikeTests
{
    [Fact]
    public void Meteor_DoesNotDamageOnActivation()
    {
        var (world, player, skill, dummy, healthBefore) = CreateScenario();

        TickUntilActivated(world, player, skill, dummy);

        Assert.Equal(healthBefore, dummy.CurrentHealth);
        Assert.DoesNotContain(world.Events.Drain(), battleEvent => battleEvent is DamageAppliedEvent);
    }

    [Fact]
    public void Meteor_DamagesDummy_WhenCollisionOverlapsImpact()
    {
        var (world, player, skill, dummy, healthBefore) = CreateScenario();
        var fallTicks = FallTicks(world, skill);

        TickUntilActivated(world, player, skill, dummy);
        Assert.Equal(healthBefore, dummy.CurrentHealth);

        for (var i = 0; i < fallTicks; i++)
        {
            world.Loop.Tick();
        }

        Assert.Equal(healthBefore - skill.Damage, dummy.CurrentHealth);
        Assert.Contains(world.Events.Drain(), battleEvent => battleEvent is DamageAppliedEvent damage
            && damage.Target.EntityId == dummy.Id
            && damage.Amount == skill.Damage);
    }

    [Fact]
    public void Meteor_Misses_WhenDummyLeavesImpactRadius()
    {
        var (world, player, skill, dummy, healthBefore) = CreateScenario();

        TickUntilActivated(world, player, skill, dummy);
        world.Movement.SetPosition(new PositionSetRequest(
            dummy.Id,
            dummy.Position + new Vector2(skill.Width + dummy.CollisionRadius + 1f, 0f),
            PositionSetReason.Reset));

        TickForFall(world, skill);

        Assert.Equal(healthBefore, dummy.CurrentHealth);
        Assert.DoesNotContain(world.Events.Drain(), battleEvent => battleEvent is DamageAppliedEvent);
    }

    [Fact]
    public void Meteor_Hits_WhenDummyWalksIntoImpactRadius()
    {
        var (world, player, skill, dummy, healthBefore) = CreateScenario();
        var impactPoint = dummy.Position;
        world.Movement.SetPosition(new PositionSetRequest(
            dummy.Id,
            new Vector2(20f, 0f),
            PositionSetReason.Reset));

        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Point, Position: impactPoint)));
        TickUntilRecovering(world, player);

        world.Movement.SetPosition(new PositionSetRequest(
            dummy.Id,
            impactPoint,
            PositionSetReason.Reset));
        TickForFall(world, skill);

        Assert.Equal(healthBefore - skill.Damage, dummy.CurrentHealth);
    }

    [Fact]
    public void Meteor_Hits_WhenCollisionEdgeTouchesImpactRadius()
    {
        var (world, player, skill, dummy, healthBefore) = CreateScenario();
        var impactPoint = new Vector2(4f, 0f);

        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Point, Position: impactPoint)));
        TickUntilRecovering(world, player);

        world.Movement.SetPosition(new PositionSetRequest(
            dummy.Id,
            new Vector2(impactPoint.X + skill.Width + dummy.CollisionRadius, 0f),
            PositionSetReason.Reset));
        TickForFall(world, skill);

        Assert.Equal(healthBefore - skill.Damage, dummy.CurrentHealth);
    }

    [Fact]
    public void Meteor_Misses_WhenCollisionEdgeIsOutsideImpactRadius()
    {
        var (world, player, skill, dummy, healthBefore) = CreateScenario();
        var impactPoint = new Vector2(4f, 0f);

        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Point, Position: impactPoint)));
        TickUntilRecovering(world, player);

        world.Movement.SetPosition(new PositionSetRequest(
            dummy.Id,
            new Vector2(impactPoint.X + skill.Width + dummy.CollisionRadius + 0.1f, 0f),
            PositionSetReason.Reset));
        TickForFall(world, skill);

        Assert.Equal(healthBefore, dummy.CurrentHealth);
    }

    private static int FallTicks(BattleWorld world, Raid.Battle.Combat.SkillDefinition skill)
    {
        return world.Settings.ToTravelTicks(
            WorldSettings.PointProjectileTravelDistance,
            skill.ProjectileSpeed);
    }

    private static void TickForFall(BattleWorld world, Raid.Battle.Combat.SkillDefinition skill)
    {
        var fallTicks = FallTicks(world, skill);
        for (var i = 0; i < fallTicks; i++)
        {
            world.Loop.Tick();
        }
    }

    private static void TickUntilActivated(
        BattleWorld world,
        PlayerEntity player,
        Raid.Battle.Combat.SkillDefinition skill,
        BattleEntity dummy)
    {
        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Point, Position: dummy.Position)));
        TickUntilRecovering(world, player);
    }

    private static void TickUntilRecovering(BattleWorld world, PlayerEntity player)
    {
        for (var i = 0; i < 80; i++)
        {
            world.Loop.Tick();
            if (player.Actions.IsRecovering)
            {
                return;
            }
        }

        Assert.Fail("Did not reach recovery after meteor activation.");
    }

    private static (BattleWorld World, PlayerEntity Player, Raid.Battle.Combat.SkillDefinition Skill, DummyEntity Dummy, float HealthBefore)
        CreateScenario()
    {
        var setup = BattleWorldFactory.Create(RaidMode.Practice, new WorldSettings
        {
            FixedDeltaMilliseconds = 50
        });
        var player = PracticeSpawn.Player(setup.World, "player-1", participantSlot: 0);
        var skill = player.FindSkill("test.meteor_strike")!;
        var dummy = setup.World.Entities.Dummies().Single();
        return (setup.World, player, skill, dummy, dummy.CurrentHealth);
    }
}
