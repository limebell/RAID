using Raid.Battle.Commands;
using Raid.Battle.Entities;

namespace Raid.Battle.Actions;

public sealed class ActionComponent
{
    public GameAction? CurrentAction { get; internal set; }

    public IWorldCommand? FollowUpCommand { get; internal set; }

    public IWorldCommand? PendingInputCommand { get; internal set; }

    public bool IsBusy => CurrentAction is not null;

    public bool IsCasting =>
        CurrentAction?.CurrentPhaseKind == ActionPhaseKind.Casting;

    public bool IsRecovering =>
        CurrentAction?.CurrentPhaseKind == ActionPhaseKind.Recovery;

    public void ClearFollowUp()
    {
        FollowUpCommand = null;
    }

    public void ClearPendingInput()
    {
        PendingInputCommand = null;
    }
}
