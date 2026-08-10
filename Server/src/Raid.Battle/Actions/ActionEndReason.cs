namespace Raid.Battle.Actions;

public enum ActionEndReason
{
    Completed = 1,
    CancelledByMove = 2,
    CancelledByInterrupt = 3,
    Failed = 4
}
