using System.Numerics;
using Raid.Battle.Entities;
using Raid.Battle.Events;
using Raid.Battle.World;

namespace Raid.Battle.Movement;

public sealed class MovementSystem(BattleWorld world)
{
    public void SetIntent(MovementIntent intent)
    {
        var entity = world.Entities.Find(intent.EntityId)
            ?? throw new InvalidOperationException($"Entity '{intent.EntityId}' was not found.");

        entity.ActiveMove = new MoveAction(intent.Destination, intent.Speed);
    }

    public void Update(float deltaTime)
    {
        foreach (var entity in world.Entities.All())
        {
            var activeMove = entity.ActiveMove;
            if (activeMove is null)
            {
                continue;
            }

            var toDestination = activeMove.Destination - entity.Position;
            var remainingDistance = toDestination.Length();
            if (remainingDistance <= activeMove.ArrivalTolerance)
            {
                CompleteMovement(entity, activeMove.Destination);
                continue;
            }

            var stepDistance = activeMove.Speed * deltaTime;
            if (stepDistance >= remainingDistance)
            {
                CompleteMovement(entity, activeMove.Destination);
                continue;
            }

            var direction = Vector2.Normalize(toDestination);
            entity.Position += direction * stepDistance;

            world.Events.Add(new EntityMovedEvent(world.Tick, entity.Id, entity.Position));
        }
    }

    private void CompleteMovement(BattleEntity entity, Vector2 destination)
    {
        entity.Position = destination;
        entity.ActiveMove = null;

        world.Events.Add(new EntityMovedEvent(world.Tick, entity.Id, entity.Position));
        world.Events.Add(new EntityMoveCompletedEvent(world.Tick, entity.Id, entity.Position));
    }
}
