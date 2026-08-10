using Raid.Battle.Combat;

namespace Raid.Battle.Actions.Phases;

public sealed class ActivationPhase : IActionPhase
{
    public ActionPhaseKind Kind => ActionPhaseKind.Activation;

    public void Enter(ActionContext context)
    {
        if (context.TargetId is null || context.Damage <= 0f)
        {
            return;
        }

        context.World.Hits.Enqueue(new HitRequest(
            context.Owner.Id,
            context.TargetId.Value,
            context.Action.SkillId,
            context.Damage));
    }

    public ActionPhaseResult Update(ActionContext context, float deltaTime)
    {
        return ActionPhaseResult.Advance;
    }

    public void Exit(ActionContext context, ActionEndReason reason)
    {
    }
}
