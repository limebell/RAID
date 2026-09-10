using Raid.Battle.Actions;
using Raid.Battle.Combat;
using Raid.Battle.Entities;

namespace Raid.Battle.Commands;

public sealed class ReleaseSkillCommand(EntityId issuerId, string skillId) : IWorldCommand
{
    public EntityId IssuerId { get; } = issuerId;

    public string SkillId { get; } = skillId;

    public CommandResult Execute(CommandContext context)
    {
        var player = context.World.Entities.Find<PlayerEntity>(IssuerId);
        if (player is null)
        {
            return CommandResult.Failure(ReleaseSkillFailureReason.PlayerNotFound);
        }

        var action = player.Actions.CurrentAction;
        if (action is null)
        {
            return CommandResult.Failure(ReleaseSkillFailureReason.NoActiveSkill);
        }

        if (action.Skill.SkillId != SkillId)
        {
            return CommandResult.Failure(ReleaseSkillFailureReason.SkillMismatch);
        }

        if (!action.Skill.RequiresRelease)
        {
            return CommandResult.Failure(ReleaseSkillFailureReason.SkillDoesNotHold);
        }

        action.ReleaseRequested = true;
        return CommandResult.Success();
    }
}
