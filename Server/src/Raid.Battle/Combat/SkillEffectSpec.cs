using Raid.Contracts.Common;

namespace Raid.Battle.Combat;

public sealed record SkillEffectSpec(
    SkillEffectTrigger Trigger,
    float Heal = 0f,
    float Shield = 0f,
    float ManaRestorePercentOfMax = 0f,
    StatusEffectKind? Status = null,
    int DurationMilliseconds = 0,
    float TickDamage = 0f,
    int TickIntervalMilliseconds = 0,
    float ZoneRadius = 0f,
    int ZoneDurationMilliseconds = 0,
    int ZoneTickIntervalMilliseconds = 0,
    float ZoneTickDamage = 0f,
    float ZoneTickHeal = 0f,
    float ZoneManaRestoreOnEnemyDamage = 0f,
    string? BuffId = null);
