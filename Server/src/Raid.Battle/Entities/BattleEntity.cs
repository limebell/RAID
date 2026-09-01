using System.Numerics;
using Raid.Battle.Actions;
using Raid.Battle.Movement;
using Raid.Contracts.Common;

namespace Raid.Battle.Entities;

public abstract class BattleEntity(
    EntityId id,
    EntityKind kind,
    Vector2 position,
    Vector2 facingDirection,
    float moveSpeed,
    float turnSpeedRadiansPerSecond,
    float maxHealth)
{
    public EntityId Id { get; } = id;

    public EntityKind Kind { get; } = kind;

    public Vector2 Position { get; internal set; } = position;

    public Vector2 FacingDirection { get; internal set; } = facingDirection;

    public Vector2 DesiredFacingDirection { get; internal set; } = facingDirection;

    public float MoveSpeed { get; } = moveSpeed;

    public float TurnSpeedRadiansPerSecond { get; } = turnSpeedRadiansPerSecond;

    public float MaxHealth { get; } = maxHealth;

    public float CurrentHealth { get; internal set; } = maxHealth;

    public bool IsDowned { get; internal set; }

    public MovementAction? ActiveMovement { get; internal set; }

    public ActionComponent Actions { get; } = new();
}
