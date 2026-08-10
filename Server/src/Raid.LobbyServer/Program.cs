var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    service = "Raid.LobbyServer",
    status = "running"
}));

app.Run();
