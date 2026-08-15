using Raid.Battle.Combat;
using Raid.Battle.Entities;

namespace Raid.Battle.Actions.Phases;

public sealed class ActivationPhase : IActionPhase
{
    public ActionPhaseKind Kind => ActionPhaseKind.Activation;

    public void Enter(ActionContext context)
    {
        if (context.Owner is PlayerEntity player)
        {
            context.World.Cooldowns.StartCooldown(
                player,
                context.Action.SkillId,
                context.Action.CooldownMilliseconds);

            if (context.Action.ManaCost > 0
                && !context.World.Resources.TrySpendMana(player, context.Action.ManaCost))
            {
                return;
            }
        }

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
