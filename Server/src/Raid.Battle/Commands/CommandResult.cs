namespace Raid.Battle.Commands;

public readonly record struct CommandResult(
    bool Succeeded,
    UseSkillFailureReason? UseSkillFailure = null,
    MoveFailureReason? MoveFailure = null)
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
}
