using System.Numerics;
using Raid.Battle.Entities;
using Raid.Battle.Movement;

namespace Raid.Battle.Events;

public sealed record PositionSetEvent(
    long Tick,
    EntityId EntityId,
    Vector2 Position,
    PositionSetReason Reason) : IBattleEvent;
