using Raid.Battle.Entities;
using Raid.Battle.Snapshots;

namespace Raid.Battle.Events;

public sealed record EntityMovedEvent(
    long Tick,
    EntitySnapshot Entity) : IBattleEvent
{
    public static EntityMovedEvent FromEntity(BattleEntity entity, long tick)
    {
        return new EntityMovedEvent(tick, EntitySnapshot.FromEntity(entity));
    }
}
