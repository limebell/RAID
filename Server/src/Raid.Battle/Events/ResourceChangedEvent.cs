using Raid.Battle.Snapshots;

namespace Raid.Battle.Events;

public sealed record ResourceChangedEvent(
    long Tick,
    EntitySnapshot Entity) : IBattleEvent;
