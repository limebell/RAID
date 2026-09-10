using System.Numerics;

namespace Raid.Battle.Definitions;

public sealed record SpawnPoint(
    Vector2 Position,
    Vector2? FacingDirection = null,
    string? DefinitionId = null);
