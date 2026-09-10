using System.Numerics;
using Raid.Battle.Actions;
using Raid.Battle.Commands;
using Raid.Battle.Effects;
using Raid.Battle.Entities;
using Raid.Battle.Events;
using Raid.Battle.World;
using Raid.Contracts.Common;

namespace Raid.Battle.Tests;

public sealed class ZoneConstraintDashTests
{
    [Fact]
    public void ZoneBurn_DamagesDummyAndRestoresMana()
    {
        var (world, player) = CreatePracticeWithPlayer(highManaRegen: false);
        var skill = player.FindSkill("test.zone_burn")!;
        var dummy = world.Entities.Dummies().Single();
        var healthBefore = dummy.CurrentHealth;
        player.CurrentMana = skill.ManaCost;

        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Point, Position: dummy.Position)));
        TickUntil(world, () => dummy.CurrentHealth < healthBefore);

        Assert.Equal(healthBefore - 30f, dummy.CurrentHealth);
        Assert.Equal(15f, player.CurrentMana);
        Assert.Contains(world.Events.Drain(), battleEvent => battleEvent is ZoneSpawnedEvent spawned
            && spawned.SkillId == skill.SkillId);
    }

    [Fact]
    public void ZoneMend_HealsPlayerInside()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.zone_mend")!;
        player.CurrentHealth = 800f;

        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Point, Position: player.Position)));
        TickUntil(world, () => player.CurrentHealth > 800f);

        Assert.Equal(840f, player.CurrentHealth);
    }

    [Fact]
    public void StunBolt_AppliesStunOnHit()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.stun_bolt")!;
        var dummy = world.Entities.Dummies().Single();

        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id)));
        TickUntil(world, () => world.Effects.Has(dummy.Id, StatusEffectKind.Stun));

        Assert.True(world.Effects.Has(dummy.Id, StatusEffectKind.Stun));
    }

    [Fact]
    public void Stun_BlocksMoveAndSkills()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var dummy = world.Entities.Dummies().Single();

        Assert.True(world.Effects.Apply(new StatusEffect(
            player.Id,
            StatusEffectKind.Stun,
            player.Id,
            "test.stun_bolt",
            1f)));

        var move = new MoveCommand(player.Id, new Vector2(8f, 0f))
            .Execute(new CommandContext(world));
        Assert.False(move.Succeeded);
        Assert.Equal(MoveFailureReason.PlayerStunned, move.MoveFailure);

        var skill = player.FindSkill("test.instant_strike")!;
        var use = new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id))
            .Execute(new CommandContext(world));
        Assert.False(use.Succeeded);
        Assert.Equal(UseSkillFailureReason.PlayerStunned, use.UseSkillFailure);
    }

    [Fact]
    public void Silence_BlocksSkills_ButAllowsMove()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var dummy = world.Entities.Dummies().Single();

        Assert.True(world.Effects.Apply(new StatusEffect(
            player.Id,
            StatusEffectKind.Silence,
            player.Id,
            "test.stun_bolt",
            1f)));

        var move = new MoveCommand(player.Id, new Vector2(3f, 0f))
            .Execute(new CommandContext(world));
        Assert.True(move.Succeeded);

        var skill = player.FindSkill("test.instant_strike")!;
        var use = new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id))
            .Execute(new CommandContext(world));
        Assert.False(use.Succeeded);
        Assert.Equal(UseSkillFailureReason.PlayerSilenced, use.UseSkillFailure);
    }

    [Fact]
    public void Stun_CancelsCastingWithoutSpendingMana()
    {
        var (world, player) = CreatePracticeWithPlayer(highManaRegen: false);
        var skill = player.FindSkill("test.meteor_strike")!;
        var dummy = world.Entities.Dummies().Single();
        player.CurrentMana = skill.ManaCost;

        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Point, Position: dummy.Position)));
        TickUntil(world, () => player.Actions.IsCasting);

        Assert.True(world.Effects.Apply(new StatusEffect(
            player.Id,
            StatusEffectKind.Stun,
            dummy.Id,
            "test.stun_bolt",
            1f)));

        Assert.False(player.Actions.IsBusy);
        Assert.Equal(skill.ManaCost, player.CurrentMana);
    }

    [Fact]
    public void HoldGuard_Immunity_BlocksStun()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var hold = player.FindSkill("test.hold_guard")!;

        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            hold,
            new SkillTarget(SkillTargetingMode.None)));
        TickUntil(world, () => player.Actions.CurrentAction?.CurrentPhaseKind == ActionPhaseKind.Holding);

        Assert.False(world.Effects.Apply(new StatusEffect(
            player.Id,
            StatusEffectKind.Stun,
            player.Id,
            "test.stun_bolt",
            1f)));
        Assert.Equal(ActionPhaseKind.Holding, player.Actions.CurrentAction?.CurrentPhaseKind);
    }

    [Fact]
    public void Dash_InterruptsWindup_ConsumesCancelledSkillAndMoves()
    {
        var (world, player) = CreatePracticeWithPlayer(highManaRegen: false);
        var strike = player.FindSkill("test.instant_strike")!;
        var dash = player.FindSkill("test.dash")!;
        var dummy = world.Entities.Dummies().Single();
        var healthBefore = dummy.CurrentHealth;
        player.CurrentMana = strike.ManaCost;
        var start = player.Position;

        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            strike,
            new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id)));
        TickUntil(world, () => player.Actions.CurrentAction?.CurrentPhaseKind == ActionPhaseKind.Windup);

        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            dash,
            new SkillTarget(SkillTargetingMode.Direction, Direction: Vector2.UnitY)));
        var destination = start + new Vector2(0f, 4f);
        TickUntil(world, () => Vector2.Distance(player.Position, destination) <= 0.05f);

        Assert.Equal(0f, player.CurrentMana);
        Assert.True(player.SkillCooldownRemainingSeconds.ContainsKey(strike.SkillId));
        Assert.Equal(healthBefore, dummy.CurrentHealth);
        Assert.Equal(destination, player.Position);
        Assert.Contains(world.Events.Drain(), battleEvent => battleEvent is ActionEndedEvent ended
            && ended.SkillId == strike.SkillId
            && ended.Reason == ActionEndReason.CancelledByInterrupt);
    }

    private static void TickUntil(BattleWorld world, Func<bool> condition)
    {
        for (var i = 0; i < 80; i++)
        {
            if (condition())
            {
                return;
            }

            world.Loop.Tick();
        }

        Assert.Fail("Condition was not met.");
    }

    private static (BattleWorld World, PlayerEntity Player) CreatePracticeWithPlayer(bool highManaRegen = true)
    {
        var settings = new WorldSettings();
        settings.Practice.HighManaRegen = highManaRegen;
        var setup = BattleWorldFactory.Create(RaidMode.Practice, settings);
        var player = PracticeSpawn.Player(setup.World, "player-1", participantSlot: 0);
        return (setup.World, player);
    }
}
