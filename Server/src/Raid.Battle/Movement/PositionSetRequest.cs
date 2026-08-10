using System.Numerics;
using Raid.Battle.Entities;

namespace Raid.Battle.Movement;

public sealed record PositionSetRequest(
    EntityId EntityId,
    Vector2 Position,
    PositionSetReason Reason);
