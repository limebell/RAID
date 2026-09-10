using System.Numerics;
using Raid.Battle.Definitions;
using Raid.Contracts.Common;

namespace Raid.Battle.Entities;

public sealed class DummyEntity : BattleEntity
{
    public DummyEntity(
        EntityId id,
        Vector2 position,
        EntityDefinition definition)
        : base(
            id,
            EntityKind.Dummy,
            position,
            Vector2.UnitX,
            definition.MoveSpeed,
            definition.TurnSpeedRadiansPerSecond,
            definition.MaxHealth)
    {
        DefinitionId = definition.DefinitionId;
        CollisionRadius = definition.CollisionRadius;
    }

    public override string DefinitionId { get; }
}
