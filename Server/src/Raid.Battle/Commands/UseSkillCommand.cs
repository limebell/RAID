using System.Numerics;
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
            return CommandResult.Failure("Player was not found.");
        }

        if (player.Actions.IsRecovering)
        {
            context.World.Actions.QueuePendingInput(player, this);
            return CommandResult.Success();
        }

        if (player.Actions.IsBusy)
        {
            return CommandResult.Failure("Player is already acting.");
        }

        var validation = ValidateTarget(context, player);
        if (!validation.Succeeded)
        {
            return validation;
        }

        var action = SkillActionFactory.Create(
            context.World.Actions.NextActionId(),
            player.Id,
            Skill,
            Target ?? new SkillTarget(SkillTargetingMode.None),
            context.World.Settings);

        return context.World.Actions.TryStart(player, action)
            ? CommandResult.Success()
            : CommandResult.Failure("Failed to start skill action.");
    }

    private CommandResult ValidateTarget(CommandContext context, PlayerEntity player)
    {
        if (Skill.TargetingMode == SkillTargetingMode.None)
        {
            return Target is null || Target.Mode == SkillTargetingMode.None
                ? CommandResult.Success()
                : CommandResult.Failure("This skill does not accept a target.");
        }

        if (Target is null)
        {
            return CommandResult.Failure("Skill target is required.");
        }

        if (Target.Mode != Skill.TargetingMode)
        {
            return CommandResult.Failure("Skill target mode does not match the skill definition.");
        }

        return Target.Mode switch
        {
            SkillTargetingMode.Entity => ValidateEntityTarget(context, player),
            SkillTargetingMode.Point => ValidatePointTarget(player),
            SkillTargetingMode.Direction => ValidateDirectionTarget(),
            _ => CommandResult.Failure("Unsupported targeting mode.")
        };
    }

    private CommandResult ValidateEntityTarget(CommandContext context, PlayerEntity player)
    {
        if (Target!.EntityId is null)
        {
            return CommandResult.Failure("Target entity id is required.");
        }

        var entity = context.World.Entities.Find(Target.EntityId.Value);
        if (entity is null)
        {
            return CommandResult.Failure("Target was not found.");
        }

        var distance = Vector2.Distance(player.Position, entity.Position);
        if (distance > Skill.Range)
        {
            return CommandResult.Failure("Target is out of range.");
        }

        return CommandResult.Success();
    }

    private CommandResult ValidatePointTarget(PlayerEntity player)
    {
        if (Target!.Position is null)
        {
            return CommandResult.Failure("Target position is required.");
        }

        var distance = Vector2.Distance(player.Position, Target.Position.Value);
        if (distance > Skill.Range)
        {
            return CommandResult.Failure("Target point is out of range.");
        }

        return CommandResult.Success();
    }

    private CommandResult ValidateDirectionTarget()
    {
        if (Target!.Direction is null)
        {
            return CommandResult.Failure("Target direction is required.");
        }

        if (Target.Direction.Value == Vector2.Zero)
        {
            return CommandResult.Failure("Target direction must be non-zero.");
        }

        return CommandResult.Success();
    }
}
