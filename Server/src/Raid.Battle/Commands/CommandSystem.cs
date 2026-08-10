using System.Collections.Concurrent;
using Raid.Battle.World;

namespace Raid.Battle.Commands;

public sealed class CommandSystem(BattleWorld world)
{
    private readonly ConcurrentQueue<IWorldCommand> _commands = [];

    public int Count => _commands.Count;

    public void Enqueue(IWorldCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        _commands.Enqueue(command);
    }

    public void ProcessQueuedCommands()
    {
        var context = new CommandContext(world);

        while (_commands.TryDequeue(out var command))
        {
            command.Execute(context);
        }
    }
}
