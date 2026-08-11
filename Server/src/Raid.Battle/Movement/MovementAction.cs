using System.Numerics;

namespace Raid.Battle.Movement;

public sealed record MovementAction(
    Vector2 Destination,
    Vector2 DesiredFacingDirection,
    float MoveSpeed,
    float TurnSpeedRadiansPerSecond,
    MovementFacingPolicy FacingPolicy,
    float ArrivalTolerance = 0.01f,
    float FacingToleranceRadians = 0.02f);
