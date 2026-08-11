using Raid.Battle.Commands;
using Raid.Battle.Entities;
using Raid.Battle.World;

namespace Raid.Battle.Actions;

public sealed class ActionSystem(BattleWorld world)
{
    private long _nextActionId = 1;

    public ActionId NextActionId()
    {
        return new ActionId(_nextActionId++);
    }

    public bool TryStart(BattleEntity owner, GameAction action)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(action);

        if (owner.Actions.CurrentAction is not null)
        {
            return false;
        }

        owner.ActiveMovement = null;
        owner.Actions.CurrentAction = action;

        var context = new ActionContext(world, owner, action)
        {
            TargetId = action.TargetId,
            Damage = action.Damage
        };
        action.Start(context);
        return true;
    }

    public void Cancel(BattleEntity owner, ActionEndReason reason)
    {
        var action = owner.Actions.CurrentAction;
        if (action is null)
        {
            return;
        }

        action.Cancel(reason);
        owner.Actions.CurrentAction = null;
        owner.Actions.ClearPendingInput();
    }

    public void Update(float deltaTime)
    {
        foreach (var entity in world.Entities.All())
        {
            var action = entity.Actions.CurrentAction;
            if (action is null)
            {
                continue;
            }

            var result = action.Update(deltaTime);
            if (result == ActionUpdateResult.Running)
            {
                continue;
            }

            entity.Actions.CurrentAction = null;
            TryExecutePendingInput(entity);
        }
    }

    public void QueuePendingInput(BattleEntity owner, IWorldCommand command)
    {
        owner.Actions.PendingInputCommand = command;
    }

    private void TryExecutePendingInput(BattleEntity entity)
    {
        var pending = entity.Actions.PendingInputCommand;
        if (pending is null)
        {
            return;
        }

        entity.Actions.ClearPendingInput();
        world.Commands.Enqueue(pending);
    }
}
