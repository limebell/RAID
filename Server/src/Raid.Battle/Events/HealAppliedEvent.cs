using Raid.Battle.Entities;
using Raid.Battle.Snapshots;

namespace Raid.Battle.Events;

public sealed record HealAppliedEvent(
    long Tick,
    EntitySnapshot Target,
    EntityId SourceId,
    string SkillId,
    float Amount) : IBattleEvent;
