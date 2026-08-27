using Raid.Contracts.Common;

namespace Raid.Contracts.Battle;

public sealed record SkillInfoDto(
    string SkillId,
    SkillTargetingMode TargetingMode,
    int ManaCost,
    float Range,
    float Width);
