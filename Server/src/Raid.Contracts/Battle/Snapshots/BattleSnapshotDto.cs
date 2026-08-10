using Raid.Contracts.Common;

namespace Raid.Contracts.Battle.Snapshots;

public sealed record BattleSnapshotDto(
    Guid SessionId,
    RaidMode Mode,
    long Tick,
    IReadOnlyList<EntitySnapshotDto> Entities);
