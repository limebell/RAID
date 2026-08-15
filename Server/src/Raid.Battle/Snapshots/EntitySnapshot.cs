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
    float CurrentMana,
    float MaxMana,
    bool IsBusy,
    string? CurrentPhase)
{
    public static EntitySnapshot FromEntity(BattleEntity entity)
    {
        var (currentMana, maxMana) = entity switch
        {
            PlayerEntity player => (player.CurrentMana, player.MaxMana),
            _ => (0f, 0f)
        };

        return new EntitySnapshot(
            entity.Id,
            entity.Kind,
            entity.Position,
            entity.FacingDirection,
            entity.CurrentHealth,
            entity.MaxHealth,
            currentMana,
            maxMana,
            entity.Actions.IsBusy,
            entity.Actions.CurrentAction?.CurrentPhaseKind?.ToString());
    }
}
