using Raid.Battle.World;
using Raid.GameServer.Sessions;

namespace Raid.GameServer.Scheduling;

public sealed class RaidScheduler
{
    public void Tick(RaidSession session)
    {
        Tick(session.World);
    }

    public void Tick(BattleWorld world)
    {
        world.Loop.Tick();
    }
}
