using Raid.Battle.Entities;
using Raid.Battle.Snapshots;

namespace Raid.Battle.Events;

public sealed record DamageAppliedEvent(
    long Tick,
    EntitySnapshot Target,
    EntityId AttackerId,
    string SkillId,
    float Amount) : IBattleEvent;
