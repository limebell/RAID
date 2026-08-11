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
            var moved = TryMove(entity, activeMovement, deltaTime);
            var reachedDestination = HasReachedDestination(entity, activeMovement);
            var facingAligned = IsFacingAligned(entity, activeMovement);

            if (!reachedDestination || !facingAligned)
            {
                if (moved || rotationChanged)
                {
                    world.Events.Add(new EntityMovedEvent(
                        world.Tick,
                        entity.Id,
                        entity.Position,
                        entity.FacingDirection));
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
                world.Events.Add(new EntityMovedEvent(
                    world.Tick,
                    entity.Id,
                    entity.Position,
                    entity.FacingDirection));
            }

            return;
        }

        if (emitMovedEvent)
        {
            world.Events.Add(new EntityMovedEvent(world.Tick, entity.Id, entity.Position, entity.FacingDirection));
        }

        world.Events.Add(new EntityMoveCompletedEvent(world.Tick, entity.Id, entity.Position, entity.FacingDirection));
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

    private static bool TryMove(BattleEntity entity, MovementAction activeMovement, float deltaTime)
    {
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
            entity.Position = destination;
            return false;
        }

        var stepDistance = activeMovement.MoveSpeed * deltaTime;
        if (stepDistance >= remainingDistance)
        {
            entity.Position = destination;
            return true;
        }

        var direction = Vector2.Normalize(toDestination);
        entity.Position += direction * stepDistance;
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
