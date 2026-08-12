using Raid.Battle.Entities;
using Raid.Battle.Movement;
using Raid.Battle.Snapshots;

namespace Raid.Battle.Events;

public sealed record PositionSetEvent(
    long Tick,
    EntitySnapshot Entity,
    PositionSetReason Reason) : IBattleEvent
{
    public static PositionSetEvent FromEntity(BattleEntity entity, long tick, PositionSetReason reason)
    {
        return new PositionSetEvent(tick, EntitySnapshot.FromEntity(entity), reason);
    }
}
