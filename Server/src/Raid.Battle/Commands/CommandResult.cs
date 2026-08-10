namespace Raid.Battle.Commands;

public readonly record struct CommandResult(bool Succeeded, string? FailureReason = null)
{
    public static CommandResult Success()
    {
        return new(true);
    }

    public static CommandResult Failure(string reason)
    {
        return new(false, reason);
    }
}
