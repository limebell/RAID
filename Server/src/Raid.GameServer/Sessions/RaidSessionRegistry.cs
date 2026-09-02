using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Raid.Battle.World;
using Raid.Contracts.Common;
using Raid.GameServer.Options;

namespace Raid.GameServer.Sessions;

public sealed class RaidSessionRegistry(
    IOptions<SimulationOptions> simulationOptions,
    ILogger<RaidSessionRegistry> logger)
{
    private readonly ConcurrentDictionary<Guid, RaidSession> _sessions = [];
    private readonly ConcurrentDictionary<string, Guid> _userSessions = [];
    private readonly ConcurrentDictionary<string, string> _connectionUsers = [];
    private readonly ConcurrentDictionary<string, string> _userConnections = [];

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
        return _sessions.Values.Where(session => !session.IsClosed).ToArray();
    }

    public bool Remove(Guid sessionId)
    {
        return _sessions.TryRemove(sessionId, out _);
    }

    public void Bind(string connectionId, string userId, Guid sessionId)
    {
        if (_userConnections.TryGetValue(userId, out var previousConnectionId)
            && previousConnectionId != connectionId)
        {
            _connectionUsers.TryRemove(previousConnectionId, out _);
        }

        _connectionUsers[connectionId] = userId;
        _userConnections[userId] = connectionId;
        _userSessions[userId] = sessionId;
    }

    public bool TryGetByConnection(string connectionId, out RaidSession? session, out string? userId)
    {
        session = null;
        userId = null;
        if (!_connectionUsers.TryGetValue(connectionId, out userId)
            || !_userSessions.TryGetValue(userId, out var sessionId))
        {
            return false;
        }

        session = Find(sessionId);
        return session is not null;
    }

    public void UnbindConnection(string connectionId)
    {
        if (!_connectionUsers.TryRemove(connectionId, out var userId))
        {
            return;
        }

        if (_userConnections.TryGetValue(userId, out var currentConnectionId)
            && currentConnectionId != connectionId)
        {
            return;
        }

        _userConnections.TryRemove(userId, out _);
        _userSessions.TryRemove(userId, out var sessionId);
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            return;
        }

        if (session.Disconnect(userId))
        {
            logger.LogInformation(
                "Closed session {SessionId}; no connected players remain",
                session.Id);
        }
    }
}
