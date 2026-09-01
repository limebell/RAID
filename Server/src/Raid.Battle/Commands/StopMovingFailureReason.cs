namespace Raid.Battle.Commands;

public enum StopMovingFailureReason
{
    PlayerNotFound = 1, // Player was not found.
    PlayerAlreadyActing = 2 // Player is already acting.
}
