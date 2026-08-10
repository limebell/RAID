using Raid.Battle.Entities;

namespace Raid.Battle.Combat;

public sealed record HitRequest(
    EntityId AttackerId,
    EntityId TargetId,
    string SkillId,
    float Damage);
