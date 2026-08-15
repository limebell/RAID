using Raid.Contracts.Common;

namespace Raid.Battle.Combat;

public sealed record SkillDefinition(
    string SkillId,
    SkillKind Kind,
    SkillTargetingMode TargetingMode,
    float Range,
    float Damage,
    int ManaCost,
    int CooldownMilliseconds,
    int CastTimeMilliseconds,
    int WindupTimeMilliseconds,
    int RecoveryTimeMilliseconds,
    int ChannelTimeMilliseconds = 0)
{
    public bool IsInstant => Kind == SkillKind.Instant || CastTimeMilliseconds <= 0;
}
