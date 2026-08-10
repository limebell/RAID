using Raid.Battle.Entities;

namespace Raid.Battle.Events;

public sealed record DamageAppliedEvent(
    long Tick,
    EntityId AttackerId,
    EntityId TargetId,
    string SkillId,
    float Amount,
    float TargetRemainingHealth) : IBattleEvent;
