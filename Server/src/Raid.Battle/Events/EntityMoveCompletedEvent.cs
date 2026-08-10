using System.Numerics;
using Raid.Battle.Entities;

namespace Raid.Battle.Events;

public sealed record EntityMoveCompletedEvent(
    long Tick,
    EntityId EntityId,
    Vector2 Position) : IBattleEvent;
