using System.Numerics;
using Raid.Battle.Commands;
using Raid.Battle.Entities;
using Raid.Battle.Events;
using Raid.Battle.Movement;
using Raid.Battle.World;
using Raid.Contracts.Common;

namespace Raid.Battle.Tests;

public sealed class ProjectileHitTests
{
    [Fact]
    public void InstantProjectile_DoesNotDamageOnActivation()
    {
        var (world, player, skill, dummy, healthBefore) = CreateStrikeScenario();

        TickUntilActivated(world, player, skill, dummy);

        Assert.Equal(healthBefore, dummy.CurrentHealth);
        Assert.DoesNotContain(world.Events.Drain(), battleEvent => battleEvent is DamageAppliedEvent);
    }

    [Fact]
    public void InstantProjectile_DamagesAfterTravelTime()
    {
        var (world, player, skill, dummy, healthBefore) = CreateStrikeScenario();
        var travelTicks = TravelTicks(world, player, dummy, skill);

        TickUntilActivated(world, player, skill, dummy);
        Assert.Equal(healthBefore, dummy.CurrentHealth);

        for (var i = 0; i < travelTicks; i++)
        {
            world.Loop.Tick();
        }

        Assert.Equal(healthBefore - skill.Damage, dummy.CurrentHealth);
        Assert.Contains(world.Events.Drain(), battleEvent => battleEvent is DamageAppliedEvent damage
            && damage.Target.EntityId == dummy.Id
            && damage.Amount == skill.Damage
            && damage.SkillId == skill.SkillId);
    }

    [Fact]
    public void InstantProjectile_CanFinishActionBeforeHit()
    {
        var (world, player, skill, dummy, healthBefore) = CreateStrikeScenario();
        world.Movement.SetPosition(new PositionSetRequest(
            dummy.Id,
            new Vector2(skill.Range, 0f),
            PositionSetReason.Reset));
        var travelTicks = TravelTicks(world, player, dummy, skill);

        world.Commands.Enqueue(UseStrike(player, skill, dummy));
        do
        {
            world.Loop.Tick();
        }
        while (player.Actions.IsBusy);

        Assert.False(player.Actions.IsBusy);
        Assert.Equal(healthBefore, dummy.CurrentHealth);

        for (var i = 0; i < travelTicks; i++)
        {
            world.Loop.Tick();
            if (dummy.CurrentHealth < healthBefore)
            {
                break;
            }
        }

        Assert.Equal(healthBefore - skill.Damage, dummy.CurrentHealth);
    }

    [Fact]
    public void InstantProjectile_StillHits_WhenTargetMoves()
    {
        var (world, player, skill, dummy, healthBefore) = CreateStrikeScenario();

        TickUntilActivated(world, player, skill, dummy);
        world.Movement.SetPosition(new PositionSetRequest(
            dummy.Id,
            new Vector2(skill.Range + dummy.CollisionRadius + 4f, 0f),
            PositionSetReason.Reset));

        TickUntilDamaged(world, dummy, healthBefore);

        Assert.Equal(healthBefore - skill.Damage, dummy.CurrentHealth);
    }

    [Fact]
    public void InstantProjectile_HitsSameTick_WhenOverlapping()
    {
        var (world, player, skill, dummy, healthBefore) = CreateStrikeScenario();
        world.Movement.SetPosition(new PositionSetRequest(
            dummy.Id,
            player.Position,
            PositionSetReason.Reset));

        TickUntilActivated(world, player, skill, dummy);

        Assert.Equal(healthBefore - skill.Damage, dummy.CurrentHealth);
    }

    private static int TravelTicks(
        BattleWorld world,
        BattleEntity player,
        BattleEntity dummy,
        Raid.Battle.Combat.SkillDefinition skill)
    {
        var distance = Vector2.Distance(player.Position, dummy.Position) - dummy.CollisionRadius;
        return world.Settings.ToTravelTicks(distance, skill.ProjectileSpeed);
    }

    private static void TickUntilActivated(
        BattleWorld world,
        PlayerEntity player,
        Raid.Battle.Combat.SkillDefinition skill,
        BattleEntity dummy)
    {
        world.Commands.Enqueue(UseStrike(player, skill, dummy));

        for (var i = 0; i < 40; i++)
        {
            world.Loop.Tick();
            if (player.Actions.IsRecovering)
            {
                return;
            }
        }

        Assert.Fail("Did not reach recovery after projectile activation.");
    }

    private static void TickUntilDamaged(BattleWorld world, BattleEntity dummy, float healthBefore)
    {
        for (var i = 0; i < 80; i++)
        {
            world.Loop.Tick();
            if (dummy.CurrentHealth < healthBefore)
            {
                return;
            }
        }

        Assert.Fail("Damage was not applied.");
    }

    private static UseSkillCommand UseStrike(
        PlayerEntity player,
        Raid.Battle.Combat.SkillDefinition skill,
        BattleEntity dummy) =>
        new(player.Id, skill, new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id));

    private static (BattleWorld World, PlayerEntity Player, Raid.Battle.Combat.SkillDefinition Skill, DummyEntity Dummy, float HealthBefore)
        CreateStrikeScenario()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.instant_strike")!;
        var dummy = world.Entities.Dummies().Single();
        return (world, player, skill, dummy, dummy.CurrentHealth);
    }

    private static (BattleWorld World, PlayerEntity Player) CreatePracticeWithPlayer()
    {
        var setup = BattleWorldFactory.Create(RaidMode.Practice, new WorldSettings
        {
            FixedDeltaMilliseconds = 50
        });
        var player = PracticeSpawn.Player(setup.World, "player-1", participantSlot: 0);
        return (setup.World, player);
    }
}
