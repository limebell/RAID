namespace Raid.ServiceContracts.Sessions;

public sealed record CreateRaidSessionRequest(
    string RoomId,
    IReadOnlyList<string> UserIds);
