using Microsoft.AspNetCore.SignalR;
using Raid.Contracts.Battle;
using Raid.GameServer.Hubs;

namespace Raid.GameServer.Sessions;

public sealed class BattleSyncService(IHubContext<BattleHub> hubContext, ILogger<BattleSyncService> logger)
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

        /*logger.LogInformation(
            "Publishing {EventCount} events for session {SessionId} at tick {Tick}: {Events}",
            events.Length,
            session.Id,
            session.World.Tick,
            string.Join(", ", events.Select(e => e.Type.ToString())));
        foreach (var eventDto in events)
        {
            logger.LogInformation("Event: {Event}", eventDto);
        }*/

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
