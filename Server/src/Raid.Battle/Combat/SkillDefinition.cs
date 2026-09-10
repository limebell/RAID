using Raid.Contracts.Common;

namespace Raid.Battle.Combat;

public sealed record SkillDefinition(
    string SkillId,
    SkillKind Kind,
    SkillTargetingMode TargetingMode,
    float Range,
    float Width,
    float Damage,
    int ManaCost,
    int CooldownMilliseconds,
    int CastTimeMilliseconds,
    int WindupTimeMilliseconds,
    int RecoveryTimeMilliseconds,
    int ChannelTimeMilliseconds = 0,
    bool LocksMovement = false,
    float ProjectileSpeed = 0f,
    bool IsBasicAttack = false,
    bool InterruptsActions = false,
    float DashDistance = 0f,
    float DashSpeed = 0f,
    bool InstantDash = false,
    string? ChainToSkillId = null,
    int ChainWindowMilliseconds = 0,
    string? ToggleOnBuffId = null,
    string? ToggleOffBuffId = null,
    string? RequiredBuffId = null,
    string? VariantGroup = null,
    IReadOnlyList<SkillEffectSpec>? Effects = null)
{
    public bool IsInstant => Kind == SkillKind.Instant;

    public bool CommitsOnStart => Kind is SkillKind.Hold or SkillKind.Charge;

    public bool RequiresRelease => Kind is SkillKind.Hold or SkillKind.Charge;

    public bool HasProjectile => ProjectileSpeed > 0f;

    public bool CanChain => !string.IsNullOrWhiteSpace(ChainToSkillId);

    public bool IsBuffToggle =>
        Kind == SkillKind.Toggle
        && !string.IsNullOrWhiteSpace(ToggleOnBuffId)
        && !string.IsNullOrWhiteSpace(ToggleOffBuffId);
}
