using Raid.Battle.Entities;
using Raid.Contracts.Common;

namespace Raid.Battle.Effects;

public sealed class StatusEffect(
    EntityId targetId,
    StatusEffectKind kind,
    EntityId? sourceId,
    string? skillId,
    float remainingSeconds,
    float tickIntervalSeconds = 0f,
    float tickDamage = 0f,
    string? buffId = null)
{
    public EntityId TargetId { get; } = targetId;

    public StatusEffectKind Kind { get; } = kind;

    public EntityId? SourceId { get; } = sourceId;

    public string? SkillId { get; } = skillId;

    public string? BuffId { get; } = buffId;

    public float RemainingSeconds { get; internal set; } = remainingSeconds;

    public float TickIntervalSeconds { get; } = tickIntervalSeconds;

    public float TickDamage { get; } = tickDamage;

    public float TickElapsedSeconds { get; internal set; }
}
