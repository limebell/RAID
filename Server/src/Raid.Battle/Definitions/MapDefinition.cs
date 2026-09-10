using Raid.Battle.Collision;

namespace Raid.Battle.Definitions;

public sealed record MapDefinition(
    string Id,
    Circle? Arena = null,
    Aabb? Bounds = null,
    IReadOnlyList<Aabb>? Obstacles = null,
    IReadOnlyList<SpawnPoint>? PlayerSpawns = null,
    IReadOnlyList<SpawnPoint>? EntitySpawns = null)
{
    public static MapDefinition Unbounded { get; } = new("unbounded");
}
