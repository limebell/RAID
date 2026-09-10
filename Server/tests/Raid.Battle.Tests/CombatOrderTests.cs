using System.Numerics;
using Raid.Battle.Combat;
using Raid.Battle.Commands;
using Raid.Battle.Entities;
using Raid.Battle.Events;
using Raid.Battle.Movement;
using Raid.Battle.World;
using Raid.Contracts.Common;

namespace Raid.Battle.Tests;

public sealed class CombatOrderTests
{
    [Fact]
    public void BasicAttack_KeepsAttacking_WhileTargetStaysInRange()
    {
        var (world, player, skill, dummy, healthBefore) = CreateBasicAttackScenario();
        world.Settings.Practice.IgnoreCooldowns = false;

        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id)));

        var hits = 0;
        for (var i = 0; i < 80; i++)
        {
            world.Loop.Tick();
            var expected = healthBefore - (skill.Damage * (hits + 1));
            if (dummy.CurrentHealth <= expected)
            {
                hits++;
                if (hits >= 2)
                {
                    break;
                }
            }
        }

        Assert.True(hits >= 2);
        Assert.Equal(CombatOrderKind.AttackTarget, player.CombatOrder.Kind);
    }

    [Fact]
    public void BasicAttack_Chases_WhenTargetIsOutOfRange()
    {
        var (world, player, skill, dummy, healthBefore) = CreateBasicAttackScenario();
        world.Movement.SetPosition(new PositionSetRequest(
            dummy.Id,
            new Vector2(skill.Range + dummy.CollisionRadius + 4f, 0f),
            PositionSetReason.Reset));

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
        Assert.Equal(CombatOrderKind.AttackTarget, player.CombatOrder.Kind);
    }

    [Fact]
    public void AttackMove_AttacksEnemyAlreadyInRange()
    {
        var (world, player, skill, dummy, healthBefore) = CreateBasicAttackScenario();

        world.Commands.Enqueue(new AttackMoveCommand(player.Id, new Vector2(20f, 0f)));

        for (var i = 0; i < 40; i++)
        {
            world.Loop.Tick();
            if (dummy.CurrentHealth < healthBefore)
            {
                break;
            }
        }

        Assert.Equal(healthBefore - skill.Damage, dummy.CurrentHealth);
        Assert.Equal(CombatOrderKind.AttackMove, player.CombatOrder.Kind);
    }

    [Fact]
    public void AttackMove_WalksUntilEnemyEntersRange()
    {
        var (world, player, skill, dummy, healthBefore) = CreateBasicAttackScenario();
        world.Movement.SetPosition(new PositionSetRequest(
            dummy.Id,
            new Vector2(12f, 0f),
            PositionSetReason.Reset));

        world.Commands.Enqueue(new AttackMoveCommand(player.Id, new Vector2(20f, 0f)));
        world.Loop.Tick();

        Assert.True(SkillCaster.IsInRange(player, dummy, skill.Range) == false);
        Assert.Equal(CombatOrderKind.AttackMove, player.CombatOrder.Kind);

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
    public void MoveCommand_CancelsAttackTarget()
    {
        var (world, player, skill, dummy, healthBefore) = CreateBasicAttackScenario();
        world.Settings.Practice.IgnoreCooldowns = false;

        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id)));

        for (var i = 0; i < 40; i++)
        {
            world.Loop.Tick();
            if (player.Actions.IsRecovering)
            {
                break;
            }
        }

        Assert.True(player.Actions.IsRecovering);
        Assert.Equal(healthBefore - skill.Damage, dummy.CurrentHealth);

        world.Commands.Enqueue(new MoveCommand(player.Id, new Vector2(-4f, 0f)));

        for (var i = 0; i < 40; i++)
        {
            world.Loop.Tick();
        }

        Assert.False(player.CombatOrder.IsActive);
        Assert.Equal(healthBefore - skill.Damage, dummy.CurrentHealth);
    }

    [Fact]
    public void StopMoving_HoldsBasicAttack_UntilNextAttackCommand()
    {
        var (world, player, skill, dummy, healthBefore) = CreateBasicAttackScenario();
        world.Settings.Practice.IgnoreCooldowns = false;

        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id)));

        for (var i = 0; i < 40; i++)
        {
            world.Loop.Tick();
            if (player.Actions.IsRecovering)
            {
                break;
            }
        }

        Assert.True(player.Actions.IsRecovering);
        Assert.Equal(healthBefore - skill.Damage, dummy.CurrentHealth);

        world.Commands.Enqueue(new StopMovingCommand(player.Id));

        for (var i = 0; i < 40; i++)
        {
            world.Loop.Tick();
        }

        Assert.False(player.CombatOrder.IsActive);
        Assert.False(player.Actions.IsBusy);
        Assert.Equal(healthBefore - skill.Damage, dummy.CurrentHealth);

        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id)));

        for (var i = 0; i < 40; i++)
        {
            world.Loop.Tick();
            if (dummy.CurrentHealth < healthBefore - skill.Damage)
            {
                break;
            }
        }

        Assert.Equal(healthBefore - (skill.Damage * 2), dummy.CurrentHealth);
    }

    private static (BattleWorld World, PlayerEntity Player, SkillDefinition Skill, DummyEntity Dummy, float HealthBefore)
        CreateBasicAttackScenario()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.basic_attack_b")!;
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
