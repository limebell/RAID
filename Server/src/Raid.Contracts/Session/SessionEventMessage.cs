using Raid.Contracts.Common;

namespace Raid.Contracts.Session;

public sealed record SessionEventMessage(
    Guid SessionId,
    RaidMode Mode,
    SessionEventDto Event);
