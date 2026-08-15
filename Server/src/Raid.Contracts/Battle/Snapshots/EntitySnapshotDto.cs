using Raid.Contracts.Common;

namespace Raid.Contracts.Battle.Snapshots;

public sealed record EntitySnapshotDto(
    long EntityId,
    string Kind,
    Vector2Dto Position,
    Vector2Dto FacingDirection,
    float CurrentHealth,
    float MaxHealth,
    float CurrentMana,
    float MaxMana,
    bool IsBusy,
    string? CurrentPhase);
