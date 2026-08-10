namespace Raid.Battle.Actions.Phases;

public sealed class TimedPhase(ActionPhaseKind kind, int durationTicks) : IActionPhase
{
    private int _ticksRemaining;

    public ActionPhaseKind Kind { get; } = durationTicks >= 0
        ? kind
        : throw new ArgumentOutOfRangeException(nameof(durationTicks));

    public int DurationTicks { get; } = durationTicks;

    public void Enter(ActionContext context)
    {
        _ticksRemaining = DurationTicks;
    }

    public ActionPhaseResult Update(ActionContext context, float deltaTime)
    {
        if (DurationTicks <= 0)
        {
            return ActionPhaseResult.Advance;
        }

        _ticksRemaining--;
        return _ticksRemaining <= 0
            ? ActionPhaseResult.Advance
            : ActionPhaseResult.Continue;
    }

    public void Exit(ActionContext context, ActionEndReason reason)
    {
    }
}
