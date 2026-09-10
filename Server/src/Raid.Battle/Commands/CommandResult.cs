namespace Raid.Battle.Commands;

public readonly record struct CommandResult(
    bool Succeeded,
    UseSkillFailureReason? UseSkillFailure = null,
    MoveFailureReason? MoveFailure = null,
    StopMovingFailureReason? StopMovingFailure = null,
    ReleaseSkillFailureReason? ReleaseSkillFailure = null)
{
    public static CommandResult Success()
    {
        return new(true);
    }

    public static CommandResult Failure(UseSkillFailureReason reason)
    {
        return new(false, UseSkillFailure: reason);
    }

    public static CommandResult Failure(MoveFailureReason reason)
    {
        return new(false, MoveFailure: reason);
    }

    public static CommandResult Failure(StopMovingFailureReason reason)
    {
        return new(false, StopMovingFailure: reason);
    }

    public static CommandResult Failure(ReleaseSkillFailureReason reason)
    {
        return new(false, ReleaseSkillFailure: reason);
    }
}
