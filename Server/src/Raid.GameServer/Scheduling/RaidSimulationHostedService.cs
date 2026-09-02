using Raid.GameServer.Options;
using Raid.GameServer.Sessions;
using Microsoft.Extensions.Options;

namespace Raid.GameServer.Scheduling;

public sealed class RaidSimulationHostedService(
    RaidSessionRegistry registry,
    RaidScheduler scheduler,
    BattleSyncService syncService,
    IOptions<SimulationOptions> simulationOptions,
    ILogger<RaidSimulationHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var period = TimeSpan.FromMilliseconds(simulationOptions.Value.FixedDeltaMilliseconds);
        using var timer = new PeriodicTimer(period);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var session in registry.All())
            {
                if (session.IsClosed)
                {
                    continue;
                }

                try
                {
                    scheduler.Tick(session);
                    await syncService.PublishTickAsync(session, stoppingToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogError(exception, "Session {SessionId} tick failed.", session.Id);
                }
            }
        }
    }
}
