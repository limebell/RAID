using System.Numerics;
using Raid.Battle.Commands;
using Raid.Battle.Definitions;
using Raid.Battle.Entities;
using Raid.Battle.Events;
using Raid.Battle.Movement;
using Raid.Battle.World;
using Raid.Contracts.Common;

namespace Raid.Battle.Tests;

public sealed class DirectionSkillTests
{
    [Fact]
    public void InstantSlash_DamagesDummyInLine_OnActivation()
    {
        var (world, player, skill, dummy, healthBefore) = CreateSlashScenario();

        TickUntilRecovering(world, player, skill, Vector2.UnitX);

        Assert.Equal(healthBefore - skill.Damage, dummy.CurrentHealth);
        Assert.Contains(world.Events.Drain(), battleEvent => battleEvent is DamageAppliedEvent damage
            && damage.Target.EntityId == dummy.Id
            && damage.Amount == skill.Damage
            && damage.SkillId == skill.SkillId);
    }

    [Fact]
    public void InstantSlash_MissesDummy_BesideTheLine()
    {
        var (world, player, skill, dummy, healthBefore) = CreateSlashScenario();
        world.Movement.SetPosition(new PositionSetRequest(
            dummy.Id,
            new Vector2(3f, 5f),
            PositionSetReason.Reset));

        TickUntilRecovering(world, player, skill, Vector2.UnitX);

        Assert.Equal(healthBefore, dummy.CurrentHealth);
        Assert.DoesNotContain(world.Events.Drain(), battleEvent => battleEvent is DamageAppliedEvent);
    }

    [Fact]
    public void InstantSlash_HitsEveryDummyInLine()
    {
        var (world, player, skill, _, _) = CreateSlashScenario();
        var first = world.CreateDummy(new Vector2(2f, 0f), EntityDefinitionLoader.Load("practice.dummy"));
        var second = world.CreateDummy(new Vector2(5f, 0f), EntityDefinitionLoader.Load("practice.dummy"));
        var firstHealth = first.CurrentHealth;
        var secondHealth = second.CurrentHealth;

        TickUntilRecovering(world, player, skill, Vector2.UnitX);

        Assert.Equal(firstHealth - skill.Damage, first.CurrentHealth);
        Assert.Equal(secondHealth - skill.Damage, second.CurrentHealth);
    }

    [Fact]
    public void Arrow_DoesNotDamageOnActivation()
    {
        var (world, player, skill, dummy, healthBefore) = CreateArrowScenario();

        TickUntilRecovering(world, player, skill, Vector2.UnitX);

        Assert.Equal(healthBefore, dummy.CurrentHealth);
        Assert.DoesNotContain(world.Events.Drain(), battleEvent => battleEvent is DamageAppliedEvent);
    }

    [Fact]
    public void Arrow_DamagesAfterTravel()
    {
        var (world, player, skill, dummy, healthBefore) = CreateArrowScenario();

        TickUntilRecovering(world, player, skill, Vector2.UnitX);
        TickUntilDamaged(world, dummy, healthBefore);

        Assert.Equal(healthBefore - skill.Damage, dummy.CurrentHealth);
        Assert.Contains(world.Events.Drain(), battleEvent => battleEvent is DamageAppliedEvent damage
            && damage.Target.EntityId == dummy.Id
            && damage.Amount == skill.Damage
            && damage.SkillId == skill.SkillId);
    }

    [Fact]
    public void Arrow_CanFinishActionBeforeHit()
    {
        var (world, player, skill, dummy, healthBefore) = CreateArrowScenario();
        world.Movement.SetPosition(new PositionSetRequest(
            dummy.Id,
            new Vector2(skill.Range, 0f),
            PositionSetReason.Reset));

        world.Commands.Enqueue(UseDirection(player, skill, Vector2.UnitX));
        do
        {
            world.Loop.Tick();
        }
        while (player.Actions.IsBusy);

        Assert.False(player.Actions.IsBusy);
        Assert.Equal(healthBefore, dummy.CurrentHealth);

        TickUntilDamaged(world, dummy, healthBefore);
        Assert.Equal(healthBefore - skill.Damage, dummy.CurrentHealth);
    }

    [Fact]
    public void Arrow_Misses_WhenDummyLeavesThePath()
    {
        var (world, player, skill, dummy, healthBefore) = CreateArrowScenario();

        TickUntilRecovering(world, player, skill, Vector2.UnitX);
        world.Movement.SetPosition(new PositionSetRequest(
            dummy.Id,
            new Vector2(5f, 8f),
            PositionSetReason.Reset));

        TickForFullTravel(world, skill);

        Assert.Equal(healthBefore, dummy.CurrentHealth);
        Assert.DoesNotContain(world.Events.Drain(), battleEvent => battleEvent is DamageAppliedEvent);
    }

    [Fact]
    public void Arrow_HitsFirstDummyOnly()
    {
        var (world, player, skill, dummy, _) = CreateArrowScenario();
        world.Movement.SetPosition(new PositionSetRequest(
            dummy.Id,
            new Vector2(0f, 8f),
            PositionSetReason.Reset));
        var first = world.CreateDummy(new Vector2(4f, 0f), EntityDefinitionLoader.Load("practice.dummy"));
        var second = world.CreateDummy(new Vector2(10f, 0f), EntityDefinitionLoader.Load("practice.dummy"));
        var firstHealth = first.CurrentHealth;
        var secondHealth = second.CurrentHealth;

        TickUntilRecovering(world, player, skill, Vector2.UnitX);
        TickUntilDamaged(world, first, firstHealth);
        TickForFullTravel(world, skill);

        Assert.Equal(firstHealth - skill.Damage, first.CurrentHealth);
        Assert.Equal(secondHealth, second.CurrentHealth);
    }

    [Fact]
    public void Arrow_Misses_WhenDummyIsBeyondRange()
    {
        var (world, player, skill, dummy, healthBefore) = CreateArrowScenario();
        world.Movement.SetPosition(new PositionSetRequest(
            dummy.Id,
            new Vector2(skill.Range + dummy.CollisionRadius + (skill.Width * 0.5f) + 1f, 0f),
            PositionSetReason.Reset));

        TickUntilRecovering(world, player, skill, Vector2.UnitX);
        TickForFullTravel(world, skill);

        Assert.Equal(healthBefore, dummy.CurrentHealth);
    }

    private static void TickUntilRecovering(
        BattleWorld world,
        PlayerEntity player,
        Raid.Battle.Combat.SkillDefinition skill,
        Vector2 direction)
    {
        world.Commands.Enqueue(UseDirection(player, skill, direction));

        for (var i = 0; i < 80; i++)
        {
            world.Loop.Tick();
            if (player.Actions.IsRecovering)
            {
                return;
            }
        }

        Assert.Fail("Did not reach recovery after direction skill activation.");
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

    private static void TickForFullTravel(BattleWorld world, Raid.Battle.Combat.SkillDefinition skill)
    {
        var ticks = world.Settings.ToTravelTicks(skill.Range, skill.ProjectileSpeed) + 2;
        for (var i = 0; i < ticks; i++)
        {
            world.Loop.Tick();
        }
    }

    private static UseSkillCommand UseDirection(
        PlayerEntity player,
        Raid.Battle.Combat.SkillDefinition skill,
        Vector2 direction) =>
        new(player.Id, skill, new SkillTarget(SkillTargetingMode.Direction, Direction: direction));

    private static (BattleWorld World, PlayerEntity Player, Raid.Battle.Combat.SkillDefinition Skill, DummyEntity Dummy, float HealthBefore)
        CreateSlashScenario()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.direction_slash")!;
        var dummy = world.Entities.Dummies().Single();
        return (world, player, skill, dummy, dummy.CurrentHealth);
    }

    private static (BattleWorld World, PlayerEntity Player, Raid.Battle.Combat.SkillDefinition Skill, DummyEntity Dummy, float HealthBefore)
        CreateArrowScenario()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.arrow_strike")!;
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
