namespace Raid.Battle.Commands;

public enum UseSkillFailureReason
{
    PlayerNotFound = 1, // Player was not found.
    PlayerAlreadyActing = 2, // Player is already acting.
    SkillOnCooldown = 3, // Skill is on cooldown.
    FailedToStartSkillAction = 4, // Failed to start skill action.
    SkillDoesNotAcceptTarget = 5, // This skill does not accept a target.
    SkillTargetRequired = 6, // Skill target is required.
    SkillTargetModeMismatch = 7, // Skill target mode does not match the skill definition.
    UnsupportedTargetingMode = 8, // Unsupported targeting mode.
    TargetEntityIdRequired = 9, // Target entity id is required.
    TargetNotFound = 10, // Target was not found.
    TargetOutOfRange = 11, // Target is out of range.
    TargetPositionRequired = 12, // Target position is required.
    TargetPointOutOfRange = 13, // Target point is out of range.
    TargetDirectionRequired = 14, // Target direction is required.
    TargetDirectionMustBeNonZero = 15 // Target direction must be non-zero.
}
