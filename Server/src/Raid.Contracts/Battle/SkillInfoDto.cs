using Raid.Contracts.Common;

namespace Raid.Contracts.Battle;

public sealed record SkillInfoDto(
    string SkillId,
    SkillKind Kind,
    SkillTargetingMode TargetingMode,
    int ManaCost,
    float Range,
    float Width,
    int CastTimeMilliseconds = 0,
    int ChannelTimeMilliseconds = 0,
    float ProjectileSpeed = 0f,
    bool IsBasicAttack = false,
    string? ChainToSkillId = null,
    int ChainWindowMilliseconds = 0,
    string? ToggleOnBuffId = null,
    string? ToggleOffBuffId = null,
    string? RequiredBuffId = null,
    string? VariantGroup = null,
    SkillBarSlot? BarSlot = null);
