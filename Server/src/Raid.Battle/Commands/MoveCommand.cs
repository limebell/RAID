using System.Numerics;
using Raid.Battle.Actions;
using Raid.Battle.Entities;
using Raid.Battle.Movement;

namespace Raid.Battle.Commands;

public sealed class MoveCommand(EntityId issuerId, Vector2 destination) : IWorldCommand
{
    public EntityId IssuerId { get; } = issuerId;

    public Vector2 Destination { get; } = destination;

    public CommandResult Execute(CommandContext context)
    {
        var player = context.World.Entities.Find<PlayerEntity>(IssuerId);
        if (player is null)
        {
            return CommandResult.Failure("Player was not found.");
        }

        if (player.Actions.IsCasting)
        {
            context.World.Actions.Cancel(player, ActionEndReason.CancelledByMove);
        }
        else if (player.Actions.IsRecovering)
        {
            context.World.Actions.QueuePendingInput(player, this);
            return CommandResult.Success();
        }
        else if (player.Actions.IsBusy)
        {
            return CommandResult.Failure("Player is already acting.");
        }

        var desiredFacingDirection = Destination - player.Position;
        if (desiredFacingDirection == Vector2.Zero)
        {
            desiredFacingDirection = player.FacingDirection;
        }
        else
        {
            desiredFacingDirection = Vector2.Normalize(desiredFacingDirection);
        }

        context.World.Movement.SetIntent(
            new MovementIntent(
                player.Id,
                Destination,
                desiredFacingDirection,
                MoveSpeed: player.MoveSpeed,
                TurnSpeedRadiansPerSecond: player.TurnSpeedRadiansPerSecond,
                FacingPolicy: MovementFacingPolicy.RotateWhileMoving));

        return CommandResult.Success();
    }
}
