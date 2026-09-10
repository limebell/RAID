using Raid.Battle.Snapshots;

namespace Raid.Battle.Events;

public sealed record ShieldChangedEvent(
    long Tick,
    EntitySnapshot Target,
    string? SkillId,
    float Amount) : IBattleEvent;
