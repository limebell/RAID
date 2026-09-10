using System.Numerics;
using Raid.Battle.Actions;
using Raid.Battle.Combat;
using Raid.Battle.Entities;
using Raid.Contracts.Common;

namespace Raid.Battle.Commands;

public sealed class UseSkillCommand(
    EntityId issuerId,
    SkillDefinition skill,
    SkillTarget? target) : IWorldCommand
{
    public EntityId IssuerId { get; } = issuerId;

    public SkillDefinition Skill { get; } = skill;

    public SkillTarget? Target { get; } = target;

    public CommandResult Execute(CommandContext context)
    {
        var player = context.World.Entities.Find<PlayerEntity>(IssuerId);
        if (player is null)
        {
            return CommandResult.Failure(UseSkillFailureReason.PlayerNotFound);
        }

        if (context.World.Effects.IsSkillLocked(player.Id))
        {
            return CommandResult.Failure(
                context.World.Effects.Has(player.Id, StatusEffectKind.Stun)
                    ? UseSkillFailureReason.PlayerStunned
                    : UseSkillFailureReason.PlayerSilenced);
        }

        var skill = context.World.Chains.ResolveFollowUp(player, Skill);
        if (context.World.Chains.IsUnavailableFollowUp(player, skill))
        {
            return CommandResult.Failure(UseSkillFailureReason.ChainNotAvailable);
        }

        skill = SkillResolver.Resolve(context.World, player, skill);
        if (SkillResolver.IsUnavailable(context.World, player, skill))
        {
            return CommandResult.Failure(UseSkillFailureReason.BuffNotAvailable);
        }

        var validation = ValidateTarget(context, player, skill);
        if (!validation.Succeeded)
        {
            return validation;
        }

        if (TryBeginCombatOrder(context, player, skill))
        {
            return CommandResult.Success();
        }

        if (skill.InterruptsActions)
        {
            if (!context.World.Cooldowns.CanUseSkill(player, skill.SkillId))
            {
                return CommandResult.Failure(UseSkillFailureReason.SkillOnCooldown);
            }

            if (player.Actions.IsBusy)
            {
                context.World.Actions.InterruptCurrent(player);
            }

            return SkillCaster.TryStart(context.World, player, skill, Target)
                ? CommandResult.Success()
                : CommandResult.Failure(UseSkillFailureReason.FailedToStartSkillAction);
        }

        if (player.Actions.IsRecovering)
        {
            context.World.Actions.QueuePendingInput(player, this);
            return CommandResult.Success();
        }

        if (player.Actions.IsBusy)
        {
            return CommandResult.Failure(UseSkillFailureReason.PlayerAlreadyActing);
        }

        if (!context.World.Cooldowns.CanUseSkill(player, skill.SkillId))
        {
            return CommandResult.Failure(UseSkillFailureReason.SkillOnCooldown);
        }

        return SkillCaster.TryStart(context.World, player, skill, Target)
            ? CommandResult.Success()
            : CommandResult.Failure(UseSkillFailureReason.FailedToStartSkillAction);
    }

    private bool TryBeginCombatOrder(CommandContext context, PlayerEntity player, SkillDefinition skill)
    {
        if (skill.TargetingMode != SkillTargetingMode.Entity || Target?.EntityId is null)
        {
            return false;
        }

        var entity = context.World.Entities.Find(Target.EntityId.Value);
        if (entity is null)
        {
            return false;
        }

        if (skill.IsBasicAttack)
        {
            player.CombatOrder.SetAttackTarget(entity.Id);
            if (!player.Actions.IsBusy)
            {
                PursueOrCastNow(context, player, entity, skill, Target);
            }

            return true;
        }

        if (!SkillCaster.IsInRange(player, entity, skill.Range))
        {
            player.CombatOrder.SetChaseCast(skill, Target);
            if (!player.Actions.IsBusy)
            {
                SkillCaster.MoveToward(context.World, player, entity.Position);
            }

            return true;
        }

        if (player.CombatOrder.Kind == CombatOrderKind.ChaseCast)
        {
            player.CombatOrder.Clear();
        }

        return false;
    }

    private static void PursueOrCastNow(
        CommandContext context,
        PlayerEntity player,
        BattleEntity entity,
        SkillDefinition skill,
        SkillTarget target)
    {
        if (SkillCaster.IsInRange(player, entity, skill.Range))
        {
            SkillCaster.TryStart(context.World, player, skill, target);
            return;
        }

        SkillCaster.MoveToward(context.World, player, entity.Position);
    }

    private CommandResult ValidateTarget(CommandContext context, PlayerEntity player, SkillDefinition skill)
    {
        if (skill.TargetingMode == SkillTargetingMode.None)
        {
            return Target is null || Target.Mode == SkillTargetingMode.None
                ? CommandResult.Success()
                : CommandResult.Failure(UseSkillFailureReason.SkillDoesNotAcceptTarget);
        }

        if (Target is null)
        {
            return CommandResult.Failure(UseSkillFailureReason.SkillTargetRequired);
        }

        if (Target.Mode != skill.TargetingMode)
        {
            return CommandResult.Failure(UseSkillFailureReason.SkillTargetModeMismatch);
        }

        return Target.Mode switch
        {
            SkillTargetingMode.Entity => ValidateEntityTarget(context),
            SkillTargetingMode.Point => ValidatePointTarget(player, skill),
            SkillTargetingMode.Direction => ValidateDirectionTarget(),
            _ => CommandResult.Failure(UseSkillFailureReason.UnsupportedTargetingMode)
        };
    }

    private CommandResult ValidateEntityTarget(CommandContext context)
    {
        if (Target!.EntityId is null)
        {
            return CommandResult.Failure(UseSkillFailureReason.TargetEntityIdRequired);
        }

        var entity = context.World.Entities.Find(Target.EntityId.Value);
        if (entity is null)
        {
            return CommandResult.Failure(UseSkillFailureReason.TargetNotFound);
        }

        return CommandResult.Success();
    }

    private CommandResult ValidatePointTarget(PlayerEntity player, SkillDefinition skill)
    {
        if (Target!.Position is null)
        {
            return CommandResult.Failure(UseSkillFailureReason.TargetPositionRequired);
        }

        var distance = Vector2.Distance(player.Position, Target.Position.Value);
        if (distance > skill.Range)
        {
            return CommandResult.Failure(UseSkillFailureReason.TargetPointOutOfRange);
        }

        return CommandResult.Success();
    }

    private CommandResult ValidateDirectionTarget()
    {
        if (Target!.Direction is null)
        {
            return CommandResult.Failure(UseSkillFailureReason.TargetDirectionRequired);
        }

        if (Target.Direction.Value == Vector2.Zero)
        {
            return CommandResult.Failure(UseSkillFailureReason.TargetDirectionMustBeNonZero);
        }

        return CommandResult.Success();
    }
}
