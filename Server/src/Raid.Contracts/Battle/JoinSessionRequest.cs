using Raid.Contracts.Common;

namespace Raid.Contracts.Battle;

public sealed record JoinSessionRequest(
    RaidMode Mode,
    Guid? SessionId = null);
