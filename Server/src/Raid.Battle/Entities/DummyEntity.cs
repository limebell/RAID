using System.Numerics;

namespace Raid.Battle.Entities;

public sealed class DummyEntity(
    EntityId id,
    Vector2 position,
    float maxHealth = 100_000f)
    : BattleEntity(id, EntityKind.Dummy, position, maxHealth);
