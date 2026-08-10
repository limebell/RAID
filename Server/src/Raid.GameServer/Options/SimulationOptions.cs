using Raid.Battle.World;

namespace Raid.GameServer.Options;

public sealed class SimulationOptions
{
    public int FixedDeltaMilliseconds { get; init; } = WorldSettings.DefaultFixedDeltaMilliseconds;
}
