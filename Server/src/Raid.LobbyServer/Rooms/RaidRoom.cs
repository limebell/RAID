namespace Raid.LobbyServer.Rooms;

public sealed class RaidRoom(string roomId, string hostUserId)
{
    public string RoomId { get; } = roomId;

    public string HostUserId { get; } = hostUserId;
}
