using System.Numerics;
using Raid.Battle.Actions;
using Raid.Battle.Movement;

namespace Raid.Battle.Entities;

public abstract class BattleEntity(
    EntityId id,
    EntityKind kind,
    Vector2 position,
    float maxHealth)
{
    public EntityId Id { get; } = id;

    public EntityKind Kind { get; } = kind;

    public Vector2 Position { get; internal set; } = position;

    public float MaxHealth { get; } = maxHealth;

    public float CurrentHealth { get; internal set; } = maxHealth;

    public MoveAction? ActiveMove { get; internal set; }

    public ActionComponent Actions { get; } = new();
}
