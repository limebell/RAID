using Raid.Battle.Actions;
using Raid.Battle.Entities;

namespace Raid.Battle.Commands;

public sealed class StopMovingCommand(EntityId issuerId) : IWorldCommand
{
    public EntityId IssuerId { get; } = issuerId;

    public CommandResult Execute(CommandContext context)
    {
        var player = context.World.Entities.Find<PlayerEntity>(IssuerId);
        if (player is null)
        {
            return CommandResult.Failure(StopMovingFailureReason.PlayerNotFound);
        }

        player.CombatOrder.Clear();
        player.Actions.ClearPendingInput();

        var current = player.Actions.CurrentAction;
        if (current is not null
            && current.Skill.IsBasicAttack
            && !player.Actions.IsRecovering)
        {
            context.World.Actions.Cancel(player, ActionEndReason.CancelledByStop);
        }

        if (!player.Actions.LocksMovement)
        {
            context.World.Movement.ClearIntent(player.Id);
        }

        return CommandResult.Success();
    }
}
