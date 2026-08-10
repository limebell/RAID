using Raid.Contracts.Common;

namespace Raid.Contracts.Battle.Snapshots;

public sealed record EntitySnapshotDto(
    long EntityId,
    string Kind,
    Vector2Dto Position,
    float CurrentHealth,
    float MaxHealth,
    bool IsBusy,
    string? CurrentPhase);
