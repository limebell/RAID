using Raid.Battle.World;
using Raid.GameServer.Sessions;

namespace Raid.GameServer.Scheduling;

public sealed class RaidScheduler
{
    public void Tick(RaidSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        Tick(session.World);
    }

    public void Tick(BattleWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        world.Loop.Tick();
    }
}
