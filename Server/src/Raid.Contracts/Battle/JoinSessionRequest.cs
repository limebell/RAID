using Raid.Contracts.Common;

namespace Raid.Contracts.Battle;

public sealed record JoinSessionRequest(
    RaidMode Mode,
    string UserId,
    Guid? SessionId = null,
    IReadOnlyList<string>? BarSkillIds = null);
