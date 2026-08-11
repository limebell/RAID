using Raid.Contracts.Common;

namespace Raid.Contracts.Battle.Snapshots;

public sealed record EntitySnapshotDto(
    long EntityId,
    string Kind,
    Vector2Dto Position,
    Vector2Dto FacingDirection,
    float CurrentHealth,
    float MaxHealth,
    bool IsBusy,
    string? CurrentPhase);
