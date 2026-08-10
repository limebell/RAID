using Raid.Contracts.Battle.Events;
using Raid.Contracts.Common;

namespace Raid.Contracts.Battle;

public sealed record BattleTickMessage(
    Guid SessionId,
    RaidMode Mode,
    long Tick,
    IReadOnlyList<BattleEventDto> Events);
