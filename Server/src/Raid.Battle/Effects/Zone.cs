using System.Numerics;
using Raid.Battle.Entities;

namespace Raid.Battle.Effects;

public sealed class Zone(
    EntityId ownerId,
    string skillId,
    Vector2 position,
    float radius,
    float remainingSeconds,
    float tickIntervalSeconds,
    float tickDamage,
    float tickHeal,
    float manaRestoreOnEnemyDamage)
{
    public EntityId OwnerId { get; } = ownerId;

    public string SkillId { get; } = skillId;

    public Vector2 Position { get; } = position;

    public float Radius { get; } = radius;

    public float RemainingSeconds { get; internal set; } = remainingSeconds;

    public float TickIntervalSeconds { get; } = tickIntervalSeconds;

    public float TickDamage { get; } = tickDamage;

    public float TickHeal { get; } = tickHeal;

    public float ManaRestoreOnEnemyDamage { get; } = manaRestoreOnEnemyDamage;

    public float TickElapsedSeconds { get; internal set; }
}
