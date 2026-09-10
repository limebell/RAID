namespace Raid.Battle.Commands;

public enum ReleaseSkillFailureReason
{
    PlayerNotFound = 1,
    NoActiveSkill = 2,
    SkillMismatch = 3,
    SkillDoesNotHold = 4
}
