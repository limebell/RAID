namespace Raid.Battle.Definitions;

public sealed record EntityDefinition(
    string DefinitionId,
    float CollisionRadius,
    float MaxHealth = 100_000f,
    float MoveSpeed = 0f,
    float TurnSpeedRadiansPerSecond = 6f);
