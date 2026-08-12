using System.Numerics;
using Raid.Battle.Entities;

namespace Raid.Battle.Snapshots;

public sealed record EntitySnapshot(
    EntityId EntityId,
    EntityKind Kind,
    Vector2 Position,
    Vector2 FacingDirection,
    float CurrentHealth,
    float MaxHealth,
    bool IsBusy,
    string? CurrentPhase)
{
    public static EntitySnapshot FromEntity(BattleEntity entity)
    {
        return new EntitySnapshot(
            entity.Id,
            entity.Kind,
            entity.Position,
            entity.FacingDirection,
            entity.CurrentHealth,
            entity.MaxHealth,
            entity.Actions.IsBusy,
            entity.Actions.CurrentAction?.CurrentPhaseKind?.ToString());
    }
}
