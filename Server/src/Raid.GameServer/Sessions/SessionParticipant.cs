using Raid.Battle.Entities;

namespace Raid.GameServer.Sessions;

public sealed class SessionParticipant(
    string connectionId,
    string userId,
    int slot,
    PlayerEntity player)
{
    public string? ConnectionId { get; private set; } = connectionId;

    public string UserId { get; } = userId;

    public int Slot { get; } = slot;

    public PlayerEntity Player { get; } = player;

    public bool IsConnected => ConnectionId is not null;

    public void BindConnection(string connectionId)
    {
        ConnectionId = connectionId;
    }

    public void UnbindConnection()
    {
        ConnectionId = null;
    }
}
