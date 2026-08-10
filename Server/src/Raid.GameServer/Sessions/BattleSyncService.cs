using Microsoft.AspNetCore.SignalR;
using Raid.Contracts.Battle;
using Raid.GameServer.Hubs;

namespace Raid.GameServer.Sessions;

public sealed class BattleSyncService(IHubContext<BattleHub> hubContext)
{
    public async Task PublishTickAsync(RaidSession session, CancellationToken cancellationToken = default)
    {
        var events = session.World.Events.Drain()
            .Select(BattleDtoMapper.ToEventDto)
            .ToArray();

        if (events.Length == 0)
        {
            return;
        }

        var message = new BattleTickMessage(
            session.Id,
            session.Mode,
            session.World.Tick,
            events);

        await hubContext.Clients
            .Group(session.GroupName)
            .SendAsync("BattleTick", message, cancellationToken);
    }
}
