using System.Numerics;
using Raid.Battle.Actions;
using Raid.Battle.Entities;

namespace Raid.Battle.Commands;

public sealed class AttackMoveCommand(EntityId issuerId, Vector2 destination) : IWorldCommand
{
    public EntityId IssuerId { get; } = issuerId;

    public Vector2 Destination { get; } = destination;

    public CommandResult Execute(CommandContext context)
    {
        var player = context.World.Entities.Find<PlayerEntity>(IssuerId);
        if (player is null)
        {
            return CommandResult.Failure(MoveFailureReason.PlayerNotFound);
        }

        if (player.Actions.LocksMovement)
        {
            return CommandResult.Failure(MoveFailureReason.PlayerAlreadyActing);
        }

        if (context.World.Effects.IsMovementLocked(player.Id))
        {
            return CommandResult.Failure(MoveFailureReason.PlayerStunned);
        }

        if (player.Actions.IsCasting)
        {
            context.World.Actions.Cancel(player, ActionEndReason.CancelledByMove);
        }
        else if (player.Actions.IsRecovering)
        {
            player.CombatOrder.SetAttackMove(Destination);
            return CommandResult.Success();
        }
        else if (player.Actions.IsBusy)
        {
            return CommandResult.Failure(MoveFailureReason.PlayerAlreadyActing);
        }

        player.CombatOrder.SetAttackMove(Destination);
        context.World.Movement.MoveTo(player, Destination);
        return CommandResult.Success();
    }
}
