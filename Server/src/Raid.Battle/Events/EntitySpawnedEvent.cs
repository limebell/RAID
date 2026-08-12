using Raid.Battle.Entities;
using Raid.Battle.Snapshots;

namespace Raid.Battle.Events;

public sealed record EntitySpawnedEvent(
    long Tick,
    EntitySnapshot Entity) : IBattleEvent
{
    public static EntitySpawnedEvent FromEntity(BattleEntity entity, long tick)
    {
        return new EntitySpawnedEvent(tick, EntitySnapshot.FromEntity(entity));
    }
}
