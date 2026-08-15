using Raid.Battle.Snapshots;

namespace Raid.Battle.Events;

public sealed record CooldownStartedEvent(
    long Tick,
    EntitySnapshot Entity,
    string SkillId,
    float DurationSeconds) : IBattleEvent;
