using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Raid.Battle.World;
using Raid.Contracts.Common;
using Raid.GameServer.Options;

namespace Raid.GameServer.Sessions;

public sealed class RaidSessionRegistry(IOptions<SimulationOptions> simulationOptions)
{
    private readonly ConcurrentDictionary<Guid, RaidSession> _sessions = [];
    private readonly ConcurrentDictionary<string, Guid> _connectionSessions = [];

    public RaidSession Create(RaidMode mode)
    {
        var settings = new WorldSettings
        {
            FixedDeltaMilliseconds = simulationOptions.Value.FixedDeltaMilliseconds
        };

        var setup = BattleWorldFactory.Create(mode, settings);
        var session = new RaidSession(Guid.NewGuid(), setup);
        _sessions[session.Id] = session;
        return session;
    }

    public RaidSession? Find(Guid sessionId)
    {
        return _sessions.GetValueOrDefault(sessionId);
    }

    public IReadOnlyCollection<RaidSession> All()
    {
        return _sessions.Values.ToArray();
    }

    public bool Remove(Guid sessionId)
    {
        return _sessions.TryRemove(sessionId, out _);
    }

    public void BindConnection(string connectionId, Guid sessionId)
    {
        _connectionSessions[connectionId] = sessionId;
    }

    public bool TryGetSessionByConnection(string connectionId, out RaidSession? session)
    {
        session = null;
        if (!_connectionSessions.TryGetValue(connectionId, out var sessionId))
        {
            return false;
        }

        session = Find(sessionId);
        return session is not null;
    }

    public void UnbindConnection(string connectionId)
    {
        if (_connectionSessions.TryRemove(connectionId, out var sessionId)
            && _sessions.TryGetValue(sessionId, out var session))
        {
            session.UnbindConnection(connectionId);
        }
    }
}
