using Raid.Battle.Combat;

namespace Raid.Battle.Actions.Phases;

public sealed class HoldingPhase(ActionPhaseKind kind, float maxDurationSeconds) : IActionPhase
{
    public ActionPhaseKind Kind { get; } = kind is ActionPhaseKind.Holding or ActionPhaseKind.Charging
        ? kind
        : throw new ArgumentOutOfRangeException(nameof(kind));

    private float _elapsedSeconds;

    public void Enter(ActionContext context)
    {
        _elapsedSeconds = 0f;
        context.Action.ChargeRatio = 0f;
        if (context.Action.LocksMovement)
        {
            context.World.Movement.ClearIntent(context.Owner.Id);
        }

        if (Kind == ActionPhaseKind.Holding)
        {
            SkillEffectApplier.Apply(
                context.World,
                SkillEffectTrigger.WhileHolding,
                context.Action.Skill,
                context.Owner,
                context.Owner);
        }
    }

    public ActionPhaseResult Update(ActionContext context, float deltaTime)
    {
        _elapsedSeconds += deltaTime;
        if (maxDurationSeconds > 0f)
        {
            context.Action.ChargeRatio = Math.Clamp(_elapsedSeconds / maxDurationSeconds, 0f, 1f);
        }
        else
        {
            context.Action.ChargeRatio = 1f;
        }

        var timedOut = maxDurationSeconds > 0f && _elapsedSeconds >= maxDurationSeconds;
        if (context.Action.ReleaseRequested || timedOut)
        {
            return ActionPhaseResult.Advance;
        }

        return ActionPhaseResult.Continue;
    }

    public void Exit(ActionContext context, ActionEndReason reason)
    {
        if (Kind == ActionPhaseKind.Holding)
        {
            SkillEffectApplier.RemoveWhileHolding(
                context.World,
                context.Owner,
                context.Action.Skill);
        }
    }
}
