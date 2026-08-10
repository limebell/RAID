using System.Numerics;

namespace Raid.Battle.Movement;

public sealed record MoveAction(
    Vector2 Destination,
    float Speed,
    float ArrivalTolerance = 0.01f);
