using Raid.Battle.Events;
using Raid.Battle.World;

namespace Raid.Battle.Movement;

public sealed class PositionCorrectionSystem(BattleWorld world)
{
    public void SetPosition(PositionSetRequest request)
    {
        var entity = world.Entities.Find(request.EntityId)
            ?? throw new InvalidOperationException($"Entity '{request.EntityId}' was not found.");

        entity.ActiveMovement = null;
        entity.Position = request.Position;
        entity.DesiredFacingDirection = entity.FacingDirection;

        world.Events.Add(new EntityMovedEvent(world.Tick, entity.Id, entity.Position, entity.FacingDirection));
        world.Events.Add(new PositionSetEvent(
            world.Tick,
            entity.Id,
            entity.Position,
            entity.FacingDirection,
            request.Reason));
    }
}
