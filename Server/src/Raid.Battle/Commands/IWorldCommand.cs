using Raid.Battle.Entities;

namespace Raid.Battle.Commands;

public interface IWorldCommand
{
    EntityId IssuerId { get; }

    CommandResult Execute(CommandContext context);
}
