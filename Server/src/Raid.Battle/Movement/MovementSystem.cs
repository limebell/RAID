using System.Numerics;
using Raid.Battle.Collision;
using Raid.Battle.Entities;
using Raid.Battle.Events;
using Raid.Battle.World;

namespace Raid.Battle.Movement;

public sealed class MovementSystem(BattleWorld world)
{
    public void MoveTo(BattleEntity entity, Vector2 destination)
    {
        var desiredFacingDirection = destination - entity.Position;
        if (desiredFacingDirection == Vector2.Zero)
        {
            desiredFacingDirection = entity.FacingDirection;
        }
        else
        {
            desiredFacingDirection = Vector2.Normalize(desiredFacingDirection);
        }

        SetIntent(
            new MovementIntent(
                entity.Id,
                destination,
                desiredFacingDirection,
                MoveSpeed: entity.MoveSpeed,
                TurnSpeedRadiansPerSecond: entity.TurnSpeedRadiansPerSecond,
                FacingPolicy: MovementFacingPolicy.RotateWhileMoving));
    }

    public void SetIntent(MovementIntent intent)
    {
        var entity = world.Entities.Find(intent.EntityId)
            ?? throw new InvalidOperationException($"Entity '{intent.EntityId}' was not found.");

        if (intent.DesiredFacingDirection == Vector2.Zero)
        {
            throw new InvalidOperationException("Movement intent requires a non-zero desired facing direction.");
        }

        var normalizedDesiredFacingDirection = Vector2.Normalize(intent.DesiredFacingDirection);

        entity.ActiveMovement = new MovementAction(
            intent.Destination,
            normalizedDesiredFacingDirection,
            intent.MoveSpeed,
            intent.TurnSpeedRadiansPerSecond,
            intent.FacingPolicy);
    }

    public void ClearIntent(EntityId entityId)
    {
        var entity = world.Entities.Find(entityId)
            ?? throw new InvalidOperationException($"Entity '{entityId}' was not found.");

        var activeMovement = entity.ActiveMovement;
        entity.ActiveMovement = null;
        entity.DesiredFacingDirection = entity.FacingDirection;

        if (activeMovement is not null && !IsTurnOnly(activeMovement))
        {
            world.Events.Add(EntityMoveCompletedEvent.FromEntity(entity, world.Tick));
        }
    }

    public void SetDirectionIntent(
        EntityId entityId,
        Vector2 desiredFacingDirection,
        MovementFacingPolicy facingPolicy = MovementFacingPolicy.TurnOnly)
    {
        var entity = world.Entities.Find(entityId)
            ?? throw new InvalidOperationException($"Entity '{entityId}' was not found.");

        if (desiredFacingDirection == Vector2.Zero)
        {
            throw new InvalidOperationException("Direction intent requires a non-zero desired facing direction.");
        }

        SetIntent(new MovementIntent(
            entity.Id,
            entity.Position,
            Vector2.Normalize(desiredFacingDirection),
            MoveSpeed: 0f,
            TurnSpeedRadiansPerSecond: entity.TurnSpeedRadiansPerSecond,
            FacingPolicy: facingPolicy));
    }

    public void SetPosition(PositionSetRequest request)
    {
        var entity = world.Entities.Find(request.EntityId)
            ?? throw new InvalidOperationException($"Entity '{request.EntityId}' was not found.");

        entity.ActiveMovement = null;
        entity.Position = request.Position;
        entity.DesiredFacingDirection = entity.FacingDirection;

        world.Events.Add(PositionSetEvent.FromEntity(entity, world.Tick, request.Reason));
    }

    public void SnapFacing(EntityId entityId, Vector2 facingDirection)
    {
        var entity = world.Entities.Find(entityId)
            ?? throw new InvalidOperationException($"Entity '{entityId}' was not found.");

        if (facingDirection == Vector2.Zero)
        {
            throw new InvalidOperationException("Snap facing requires a non-zero direction.");
        }

        var normalized = Vector2.Normalize(facingDirection);
        if (NearlyEqual(entity.FacingDirection, normalized)
            && NearlyEqual(entity.DesiredFacingDirection, normalized))
        {
            return;
        }

        entity.FacingDirection = normalized;
        entity.DesiredFacingDirection = normalized;
        world.Events.Add(PositionSetEvent.FromEntity(entity, world.Tick, PositionSetReason.SkillFacing));
    }

    public void Update(float deltaTime)
    {
        foreach (var entity in world.Entities.All())
        {
            var activeMovement = entity.ActiveMovement;
            if (activeMovement is null)
            {
                continue;
            }

            entity.DesiredFacingDirection = activeMovement.DesiredFacingDirection;

            var rotationChanged = TryRotate(entity, activeMovement, deltaTime);
            var moved = TryMove(entity, activeMovement, deltaTime, out var blocked);
            if (blocked)
            {
                CompleteMovement(entity, emitMovedEvent: rotationChanged);
                continue;
            }

            var reachedDestination = HasReachedDestination(entity, activeMovement);
            var facingAligned = IsFacingAligned(entity, activeMovement);

            if (!reachedDestination || !facingAligned)
            {
                if (moved || rotationChanged)
                {
                    world.Events.Add(EntityMovedEvent.FromEntity(entity, world.Tick));
                }

                continue;
            }

            CompleteMovement(
                entity,
                emitMovedEvent: !moved && rotationChanged);
        }
    }

    private void CompleteMovement(BattleEntity entity, bool emitMovedEvent)
    {
        var activeMovement = entity.ActiveMovement;
        entity.ActiveMovement = null;

        if (activeMovement is null || IsTurnOnly(activeMovement))
        {
            if (emitMovedEvent)
            {
                world.Events.Add(EntityMovedEvent.FromEntity(entity, world.Tick));
            }

            return;
        }

        if (emitMovedEvent)
        {
            world.Events.Add(EntityMovedEvent.FromEntity(entity, world.Tick));
        }

        world.Events.Add(EntityMoveCompletedEvent.FromEntity(entity, world.Tick));
    }

    private static bool TryRotate(BattleEntity entity, MovementAction activeMovement, float deltaTime)
    {
        var maxTurn = activeMovement.TurnSpeedRadiansPerSecond * deltaTime;
        if (maxTurn <= 0f)
        {
            return false;
        }

        var currentAngle = MathF.Atan2(entity.FacingDirection.Y, entity.FacingDirection.X);
        var desiredAngle = MathF.Atan2(entity.DesiredFacingDirection.Y, entity.DesiredFacingDirection.X);
        var deltaAngle = WrapAngle(desiredAngle - currentAngle);

        if (MathF.Abs(deltaAngle) <= activeMovement.FacingToleranceRadians)
        {
            var alignedFacing = FromAngle(desiredAngle);
            if (!NearlyEqual(entity.FacingDirection, alignedFacing))
            {
                entity.FacingDirection = alignedFacing;
                return true;
            }

            return false;
        }

        var turnStep = Math.Clamp(deltaAngle, -maxTurn, maxTurn);
        entity.FacingDirection = FromAngle(currentAngle + turnStep);
        return true;
    }

    private bool TryMove(
        BattleEntity entity,
        MovementAction activeMovement,
        float deltaTime,
        out bool blocked)
    {
        blocked = false;
        if (IsTurnOnly(activeMovement))
        {
            return false;
        }

        var destination = activeMovement.Destination;

        if (activeMovement.FacingPolicy == MovementFacingPolicy.RotateThenMove
            && !IsFacingAligned(entity, activeMovement))
        {
            return false;
        }

        var toDestination = destination - entity.Position;
        var remainingDistance = toDestination.Length();
        if (remainingDistance <= activeMovement.ArrivalTolerance)
        {
            if (!CanStep(entity, entity.Position, destination))
            {
                blocked = true;
                return false;
            }

            entity.Position = destination;
            return false;
        }

        var stepDistance = activeMovement.MoveSpeed * deltaTime;
        var nextPosition = stepDistance >= remainingDistance
            ? destination
            : entity.Position + (Vector2.Normalize(toDestination) * stepDistance);

        if (!CanStep(entity, entity.Position, nextPosition))
        {
            blocked = true;
            return false;
        }

        entity.Position = nextPosition;
        return true;
    }

    private bool CanStep(BattleEntity entity, Vector2 from, Vector2 to)
    {
        var radius = entity.CollisionRadius;
        var map = world.Map;

        if (map.Arena is Circle arena && !CollisionHelper.CircleIsInside(to, radius, arena))
        {
            return false;
        }

        if (map.Bounds is Aabb bounds && !CollisionHelper.CircleIsInside(to, radius, bounds))
        {
            return false;
        }

        foreach (var obstacle in map.Obstacles ?? [])
        {
            if (CollisionHelper.CircleIntersects(to, radius, obstacle))
            {
                return false;
            }
        }

        foreach (var other in world.Entities.All())
        {
            if (other.Id == entity.Id || other.IsDowned)
            {
                continue;
            }

            if (!CollisionHelper.AllowsEntityStep(
                    from,
                    to,
                    radius,
                    other.Position,
                    other.CollisionRadius))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasReachedDestination(BattleEntity entity, MovementAction activeMovement)
    {
        return Vector2.Distance(entity.Position, activeMovement.Destination) <= activeMovement.ArrivalTolerance;
    }

    private static bool IsFacingAligned(BattleEntity entity, MovementAction activeMovement)
    {
        var currentAngle = MathF.Atan2(entity.FacingDirection.Y, entity.FacingDirection.X);
        var desiredAngle = MathF.Atan2(entity.DesiredFacingDirection.Y, entity.DesiredFacingDirection.X);
        return MathF.Abs(WrapAngle(desiredAngle - currentAngle)) <= activeMovement.FacingToleranceRadians;
    }

    private static bool IsTurnOnly(MovementAction activeMovement)
    {
        return activeMovement.FacingPolicy == MovementFacingPolicy.TurnOnly;
    }

    private static float WrapAngle(float radians)
    {
        while (radians > MathF.PI)
        {
            radians -= MathF.Tau;
        }

        while (radians < -MathF.PI)
        {
            radians += MathF.Tau;
        }

        return radians;
    }

    private static Vector2 FromAngle(float radians)
    {
        return new Vector2(MathF.Cos(radians), MathF.Sin(radians));
    }

    private static bool NearlyEqual(Vector2 left, Vector2 right)
    {
        return Vector2.DistanceSquared(left, right) <= 0.0001f;
    }
}
