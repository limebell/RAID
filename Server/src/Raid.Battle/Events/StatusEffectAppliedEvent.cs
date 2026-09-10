using Raid.Battle.Entities;
using Raid.Battle.Snapshots;
using Raid.Contracts.Common;

namespace Raid.Battle.Events;

public sealed record StatusEffectAppliedEvent(
    long Tick,
    EntitySnapshot Target,
    EntityId? SourceId,
    string? SkillId,
    StatusEffectKind Kind,
    float DurationSeconds,
    string? BuffId = null) : IBattleEvent;
