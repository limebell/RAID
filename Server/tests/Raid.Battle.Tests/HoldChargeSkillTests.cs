using System.Numerics;
using Raid.Battle.Actions;
using Raid.Battle.Commands;
using Raid.Battle.Entities;
using Raid.Battle.Events;
using Raid.Battle.Movement;
using Raid.Battle.World;
using Raid.Contracts.Common;

namespace Raid.Battle.Tests;

public sealed class HoldChargeSkillTests
{
    [Fact]
    public void HoldSkill_SpendsManaAndStartsCooldown_OnPress()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.hold_guard")!;
        var manaBefore = player.CurrentMana;

        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.None)));
        world.Loop.Tick();

        Assert.Equal(manaBefore - skill.ManaCost, player.CurrentMana);
        Assert.True(player.SkillCooldownRemainingSeconds.ContainsKey(skill.SkillId));
        Assert.True(player.Actions.IsBusy);
        Assert.Equal(ActionPhaseKind.Windup, player.Actions.CurrentAction?.CurrentPhaseKind);

        var events = world.Events.Drain();
        Assert.Contains(events, battleEvent => battleEvent is CooldownStartedEvent started
            && started.SkillId == skill.SkillId);
    }

    [Fact]
    public void HoldSkill_DoesNotDamageUntilRelease()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.hold_guard")!;
        var dummy = world.Entities.Dummies().Single();
        var healthBefore = dummy.CurrentHealth;

        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.None)));

        TickUntilPhase(world, player, ActionPhaseKind.Holding);

        Assert.Equal(healthBefore, dummy.CurrentHealth);
        Assert.Equal(ActionPhaseKind.Holding, player.Actions.CurrentAction?.CurrentPhaseKind);
    }

    [Fact]
    public void HoldSkill_Release_DamagesNearbyDummy()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.hold_guard")!;
        var dummy = world.Entities.Dummies().Single();
        var healthBefore = dummy.CurrentHealth;

        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.None)));
        TickUntilPhase(world, player, ActionPhaseKind.Holding);

        world.Commands.Enqueue(new ReleaseSkillCommand(player.Id, skill.SkillId));
        world.Loop.Tick();

        Assert.Equal(healthBefore - skill.Damage, dummy.CurrentHealth);

        var events = world.Events.Drain();
        Assert.Contains(events, battleEvent => battleEvent is DamageAppliedEvent damage
            && damage.Target.EntityId == dummy.Id
            && damage.Amount == skill.Damage);
    }

    [Fact]
    public void HoldSkill_MoveFails_WhileHolding()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.hold_guard")!;
        var startPosition = player.Position;

        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.None)));
        TickUntilPhase(world, player, ActionPhaseKind.Holding);

        var result = new MoveCommand(player.Id, new Vector2(8f, 0f))
            .Execute(new CommandContext(world));

        Assert.False(result.Succeeded);
        Assert.Equal(MoveFailureReason.PlayerAlreadyActing, result.MoveFailure);
        Assert.Equal(startPosition, player.Position);
        Assert.Equal(ActionPhaseKind.Holding, player.Actions.CurrentAction?.CurrentPhaseKind);
    }

    [Fact]
    public void ChargeSkill_Release_ScalesDamageWithChargeTime()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.charge_shot")!;
        var dummy = world.Entities.Dummies().Single();
        var healthBefore = dummy.CurrentHealth;

        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id)));
        TickUntilPhase(world, player, ActionPhaseKind.Charging);

        for (var i = 0; i < 9; i++)
        {
            world.Loop.Tick();
        }

        var ratioBeforeRelease = player.Actions.CurrentAction!.ChargeRatio;
        world.Commands.Enqueue(new ReleaseSkillCommand(player.Id, skill.SkillId));
        world.Loop.Tick();

        var expectedRatio = Math.Clamp(
            ratioBeforeRelease + world.Settings.FixedDeltaTimeSeconds
                / (skill.ChannelTimeMilliseconds / 1000f),
            0f,
            1f);
        var expectedDamage = skill.Damage * expectedRatio;
        Assert.Equal(healthBefore - expectedDamage, dummy.CurrentHealth);
    }

    [Fact]
    public void ChargeSkill_Timeout_FiresAtFullCharge()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.charge_shot")!;
        var dummy = world.Entities.Dummies().Single();
        var healthBefore = dummy.CurrentHealth;

        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id)));

        for (var i = 0; i < 80; i++)
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
    public void ChargeSkill_Misses_WhenScaledRangeIsTooShort()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.charge_shot")!;
        var dummy = world.Entities.Dummies().Single();
        var healthBefore = dummy.CurrentHealth;

        world.Movement.SetPosition(new PositionSetRequest(
            dummy.Id,
            new Vector2(skill.Range, 0f),
            PositionSetReason.Reset));

        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id)));
        TickUntilPhase(world, player, ActionPhaseKind.Charging);

        world.Commands.Enqueue(new ReleaseSkillCommand(player.Id, skill.SkillId));
        world.Loop.Tick();

        Assert.Equal(healthBefore, dummy.CurrentHealth);
    }

    [Fact]
    public void ReleaseSkill_Fails_ForInstantSkill()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.instant_strike")!;
        var dummy = world.Entities.Dummies().Single();

        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id)));
        world.Loop.Tick();

        var result = new ReleaseSkillCommand(player.Id, skill.SkillId)
            .Execute(new CommandContext(world));

        Assert.False(result.Succeeded);
        Assert.Equal(ReleaseSkillFailureReason.SkillDoesNotHold, result.ReleaseSkillFailure);
    }

    [Fact]
    public void HoldSkill_UseSkill_DoesNotCancel()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var hold = player.FindSkill("test.hold_guard")!;
        var strike = player.FindSkill("test.instant_strike")!;
        var dummy = world.Entities.Dummies().Single();

        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            hold,
            new SkillTarget(SkillTargetingMode.None)));
        TickUntilPhase(world, player, ActionPhaseKind.Holding);

        var result = new UseSkillCommand(
            player.Id,
            strike,
            new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id))
            .Execute(new CommandContext(world));

        Assert.False(result.Succeeded);
        Assert.Equal(UseSkillFailureReason.PlayerAlreadyActing, result.UseSkillFailure);
        Assert.Equal(ActionPhaseKind.Holding, player.Actions.CurrentAction?.CurrentPhaseKind);
        Assert.Equal(hold.SkillId, player.Actions.CurrentAction?.SkillId);
    }

    private static void TickUntilPhase(BattleWorld world, PlayerEntity player, ActionPhaseKind phase)
    {
        for (var i = 0; i < 40; i++)
        {
            world.Loop.Tick();
            if (player.Actions.CurrentAction?.CurrentPhaseKind == phase)
            {
                return;
            }
        }

        Assert.Fail($"Did not reach phase {phase}.");
    }

    private static (BattleWorld World, PlayerEntity Player) CreatePracticeWithPlayer()
    {
        var setup = BattleWorldFactory.Create(RaidMode.Practice, new WorldSettings());
        var player = PracticeSpawn.Player(setup.World, "player-1", participantSlot: 0);
        return (setup.World, player);
    }
}
