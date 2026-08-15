using Raid.Battle.Snapshots;

namespace Raid.Battle.Events;

public sealed record CooldownReadyEvent(
    long Tick,
    EntitySnapshot Entity,
    string SkillId) : IBattleEvent;
