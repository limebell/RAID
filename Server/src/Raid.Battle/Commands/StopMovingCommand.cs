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

        if (player.Actions.IsBusy)
        {
            return CommandResult.Failure(StopMovingFailureReason.PlayerAlreadyActing);
        }

        context.World.Movement.ClearIntent(player.Id);
        return CommandResult.Success();
    }
}
