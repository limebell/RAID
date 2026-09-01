using System.Numerics;
using Raid.Battle.Definitions;
using Raid.Contracts.Common;

namespace Raid.Battle.Entities;

public sealed class DummyEntity(
    EntityId id,
    Vector2 position,
    float moveSpeed = 0f,
    float turnSpeedRadiansPerSecond = 6f,
    float maxHealth = 100_000f,
    string definitionId = PracticeDummyDefinition.DefinitionId)
    : BattleEntity(
        id,
        EntityKind.Dummy,
        position,
        Vector2.UnitX,
        moveSpeed,
        turnSpeedRadiansPerSecond,
        maxHealth)
{
    public override string DefinitionId { get; } = definitionId;
}
