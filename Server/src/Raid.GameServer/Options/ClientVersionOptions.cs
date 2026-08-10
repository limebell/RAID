namespace Raid.GameServer.Options;

public sealed class ClientVersionOptions
{
    public bool Enabled { get; init; } = true;

    public string RequiredVersion { get; init; } = "dev";

    public IReadOnlyList<string> SkipPaths { get; init; } =
    [
        "/",
        "/swagger"
    ];
}
