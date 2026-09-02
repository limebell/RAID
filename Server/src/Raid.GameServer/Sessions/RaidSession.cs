using Raid.Battle.Combat;
using Raid.Battle.Definitions;
using Raid.Battle.World;
using Raid.Contracts.Common;

namespace Raid.GameServer.Sessions;

public sealed class RaidSession(Guid id, BattleWorldSetup setup)
{
    private readonly Lock _gate = new();
    private readonly List<SessionParticipant> _participants = [];
    private int _nextSlot;
    private bool _closed;

    public Guid Id { get; } = id;

    public RaidMode Mode { get; } = setup.Mode;

    public BattleWorld World { get; } = setup.World;

    public string GroupName => $"session:{Id}";

    public bool IsClosed
    {
        get
        {
            lock (_gate)
            {
                return _closed;
            }
        }
    }

    public IReadOnlyCollection<SessionParticipant> Participants
    {
        get
        {
            lock (_gate)
            {
                return [.. _participants];
            }
        }
    }

    public SessionParticipant Join(
        string userId,
        PlayerClassDefinition? playerClass = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        lock (_gate)
        {
            if (_closed)
            {
                throw new InvalidOperationException($"Session '{Id}' is closed.");
            }

            var existing = FindByUserId(userId);
            if (existing is not null)
            {
                existing.Connect();
                return existing;
            }

            var slot = _nextSlot++;
            var player = BattleWorldFactory.SpawnPlayer(
                World,
                userId,
                slot,
                playerClass ?? TestClassDefinition.Create());
            var participant = new SessionParticipant(userId, slot, player);
            _participants.Add(participant);
            return participant;
        }
    }

    public bool TryGetByUserId(string userId, out SessionParticipant? participant)
    {
        lock (_gate)
        {
            participant = FindByUserId(userId);
            return participant is not null;
        }
    }

    public bool TryGetByEntityId(long entityId, out SessionParticipant? participant)
    {
        lock (_gate)
        {
            participant = _participants.Find(candidate => candidate.Player.Id.Value == entityId);
            return participant is not null;
        }
    }

    public bool Disconnect(string userId)
    {
        lock (_gate)
        {
            FindByUserId(userId)?.Disconnect();

            if (_participants.Exists(candidate => candidate.IsConnected))
            {
                return false;
            }

            Close();
            return true;
        }
    }

    private SessionParticipant? FindByUserId(string userId)
    {
        return _participants.Find(participant => participant.UserId == userId);
    }

    private void Close()
    {
        _closed = true;

        foreach (var participant in _participants)
        {
            participant.Disconnect();
        }
    }
}
