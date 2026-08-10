using Microsoft.Extensions.Options;
using Raid.Contracts.Common;
using Raid.GameServer.Options;

namespace Raid.GameServer.Middlewares;

public sealed class ClientVersionMiddleware(
    RequestDelegate next,
    IOptions<ClientVersionOptions> options,
    ILogger<ClientVersionMiddleware> logger)
{
    private const string ClientVersionHeader = "Client-Version";

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldSkip(context))
        {
            await next(context);
            return;
        }

        if (!options.Value.Enabled)
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ClientVersionHeader, out var version))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            logger.LogError("Client-Version header is required");
            await context.Response.WriteAsJsonAsync(new {
                error = "client_version_required",
                message = "Client-Version header is required"
            });
            return;
        }

        if (version.ToString() != options.Value.RequiredVersion)
        {
            context.Response.StatusCode = StatusCodes.Status426UpgradeRequired;
            logger.LogError("Client update is required");
            await context.Response.WriteAsJsonAsync(new ClientUpdateRequiredResponse(
                options.Value.RequiredVersion,
                version.ToString()));
            return;
        }

        await next(context);
    }

    private bool ShouldSkip(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        return options.Value.SkipPaths.Any(path.StartsWith);
    }
}