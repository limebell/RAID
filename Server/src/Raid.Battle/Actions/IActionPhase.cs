namespace Raid.Battle.Actions;

public interface IActionPhase
{
    ActionPhaseKind Kind { get; }

    void Enter(ActionContext context);

    ActionPhaseResult Update(ActionContext context, float deltaTime);

    void Exit(ActionContext context, ActionEndReason reason);
}
