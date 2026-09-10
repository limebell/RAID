using Raid.Contracts.Common;

namespace Raid.Contracts.Battle.Snapshots;

/// <param name="DefinitionId">
/// Archetype id within <paramref name="Kind"/> — e.g. player class id, boss id, summon template id.
/// </param>
public sealed record EntitySnapshotDto(
    long EntityId,
    EntityKind Kind,
    string DefinitionId,
    Vector2Dto Position,
    Vector2Dto FacingDirection,
    float CurrentHealth,
    float MaxHealth,
    float CurrentMana,
    float MaxMana,
    bool IsBusy,
    string? CurrentPhase,
    float CurrentShield = 0f);
