using Raid.Battle.Entities;

namespace Raid.GameServer.Sessions;

public sealed class SessionParticipant(
    string userId,
    int slot,
    PlayerEntity player)
{
    public string UserId { get; } = userId;

    public int Slot { get; } = slot;

    public PlayerEntity Player { get; } = player;

    public bool IsConnected { get; private set; } = true;

    public void Connect()
    {
        IsConnected = true;
    }

    public void Disconnect()
    {
        IsConnected = false;
    }
}
