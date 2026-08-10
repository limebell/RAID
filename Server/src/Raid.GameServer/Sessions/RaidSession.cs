using System.Collections.Concurrent;
using Raid.Battle.Combat;
using Raid.Battle.Definitions;
using Raid.Battle.World;
using Raid.Contracts.Common;

namespace Raid.GameServer.Sessions;

public sealed class RaidSession(Guid id, BattleWorldSetup setup)
{
    private readonly Lock _gate = new();
    private readonly ConcurrentDictionary<long, SessionParticipant> _participantsByEntityId = [];
    private readonly ConcurrentDictionary<string, SessionParticipant> _participantsByConnection = [];
    private int _nextSlot;

    public Guid Id { get; } = id;

    public RaidMode Mode { get; } = setup.Mode;

    public BattleWorld World { get; } = setup.World;

    public string GroupName => $"session:{Id}";

    public IReadOnlyCollection<SessionParticipant> Participants =>
        _participantsByEntityId.Values.ToArray();

    public SessionParticipant Join(
        string connectionId,
        string? userId = null,
        PlayerClassDefinition? playerClass = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        lock (_gate)
        {
            if (_participantsByConnection.TryGetValue(connectionId, out var existing))
            {
                return existing;
            }

            var resolvedUserId = string.IsNullOrWhiteSpace(userId)
                ? connectionId
                : userId;
            var slot = _nextSlot++;
            var player = BattleWorldFactory.SpawnPlayer(
                World,
                resolvedUserId,
                slot,
                playerClass ?? TestClassDefinition.Create());
            var participant = new SessionParticipant(connectionId, resolvedUserId, slot, player);

            _participantsByEntityId[player.Id.Value] = participant;
            _participantsByConnection[connectionId] = participant;
            return participant;
        }
    }

    public bool TryGetByConnection(string connectionId, out SessionParticipant? participant)
    {
        return _participantsByConnection.TryGetValue(connectionId, out participant);
    }

    public bool TryGetByEntityId(long entityId, out SessionParticipant? participant)
    {
        return _participantsByEntityId.TryGetValue(entityId, out participant);
    }

    public void UnbindConnection(string connectionId)
    {
        lock (_gate)
        {
            if (!_participantsByConnection.TryRemove(connectionId, out var participant))
            {
                return;
            }

            participant.UnbindConnection();
        }
    }
}
