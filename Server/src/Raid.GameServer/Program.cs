using Raid.GameServer.Hubs;
using Raid.GameServer.Middlewares;
using Raid.GameServer.Options;
using Raid.GameServer.Scheduling;
using Raid.GameServer.Sessions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Raid.GameServer");

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((services, configuration) => configuration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.Configure<ClientVersionOptions>(
        builder.Configuration.GetSection("ClientVersion"));
    builder.Services.Configure<SimulationOptions>(
        builder.Configuration.GetSection("Simulation"));

    builder.Services.AddControllers();
    builder.Services.AddSignalR();
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.SetIsOriginAllowed(_ => true)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());
    });

    builder.Services.AddSingleton<RaidScheduler>();
    builder.Services.AddSingleton<RaidSessionRegistry>();
    builder.Services.AddSingleton<BattleSyncService>();
    builder.Services.AddHostedService<RaidSimulationHostedService>();

    var app = builder.Build();

    app.UseMiddleware<ClientVersionMiddleware>();

    app.UseSerilogRequestLogging();
    app.UseCors();
    app.MapControllers();
    app.MapHub<BattleHub>("/hubs/battle");

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Raid.GameServer terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
