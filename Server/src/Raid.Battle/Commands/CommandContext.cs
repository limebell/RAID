using Raid.Battle.World;

namespace Raid.Battle.Commands;

public sealed class CommandContext(BattleWorld world)
{
    public BattleWorld World { get; } = world;
}
