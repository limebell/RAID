using Raid.Battle.Entities;
using Raid.Battle.Snapshots;

namespace Raid.Battle.Events;

public sealed record EntityMoveCompletedEvent(
    long Tick,
    EntitySnapshot Entity) : IBattleEvent
{
    public static EntityMoveCompletedEvent FromEntity(BattleEntity entity, long tick)
    {
        return new EntityMoveCompletedEvent(tick, EntitySnapshot.FromEntity(entity));
    }
}
