using System.Numerics;
using Raid.Battle.Entities;

namespace Raid.Battle.Events;

public sealed record EntityMovedEvent(
    long Tick,
    EntityId EntityId,
    Vector2 Position,
    Vector2 FacingDirection) : IBattleEvent;
