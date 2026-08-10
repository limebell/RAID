using System.Numerics;
using Raid.Battle.Entities;

namespace Raid.Battle.Movement;

public sealed record MovementIntent(
    EntityId EntityId,
    Vector2 Destination,
    float Speed);
