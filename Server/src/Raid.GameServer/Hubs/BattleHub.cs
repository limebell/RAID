using System.Numerics;
using Microsoft.AspNetCore.SignalR;
using Raid.Battle.Commands;
using Raid.Contracts.Battle;
using Raid.Contracts.Battle.Commands;
using Raid.Contracts.Battle.Snapshots;
using Raid.Contracts.Common;
using Raid.Contracts.Session;
using Raid.GameServer.Sessions;

namespace Raid.GameServer.Hubs;

public sealed class BattleHub(
    RaidSessionRegistry sessions,
    ILogger<BattleHub> logger) : Hub
{
    #region Session

    public async Task<JoinSessionResponse> JoinSession(JoinSessionRequest request)
    {
        RaidSession session;
        if (request.SessionId is Guid sessionId)
        {
            session = sessions.Find(sessionId)
                ?? throw new HubException($"Session '{sessionId}' was not found.");
        }
        else
        {
            if (!Enum.IsDefined(request.Mode))
            {
                throw new HubException("JoinSession requires a valid Mode (Practice or Raid).");
            }

            session = sessions.Create(request.Mode);
            logger.LogInformation(
                "Created session {SessionId} mode {Mode} for connection {ConnectionId}",
                session.Id,
                session.Mode,
                Context.ConnectionId);
        }

        var participant = session.Join(Context.ConnectionId);
        sessions.BindConnection(Context.ConnectionId, session.Id);
        await Groups.AddToGroupAsync(Context.ConnectionId, session.GroupName);

        logger.LogInformation(
            "Connection {ConnectionId} joined session {SessionId} as player {PlayerEntityId} slot {Slot}",
            Context.ConnectionId,
            session.Id,
            participant.Player.Id.Value,
            participant.Slot);

        var playerClass = participant.Player.Class;
        return new JoinSessionResponse(
            session.Id,
            session.Mode,
            participant.Player.Id.Value,
            participant.Slot,
            playerClass.ClassId,
            playerClass.Skills.Select(skill => skill.SkillId).ToArray(),
            BattleDtoMapper.ToPracticeSettingsDto(session.World.Settings.Practice),
            BattleDtoMapper.ToSnapshot(session));
    }

    public BattleSnapshotDto RequestSnapshot()
    {
        var session = RequireSession();
        logger.LogDebug(
            "Snapshot requested by connection {ConnectionId} session {SessionId}",
            Context.ConnectionId,
            session.Id);
        return BattleDtoMapper.ToSnapshot(session);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception is null)
        {
            logger.LogInformation("Connection {ConnectionId} disconnected", Context.ConnectionId);
        }
        else
        {
            logger.LogWarning(
                exception,
                "Connection {ConnectionId} disconnected with error",
                Context.ConnectionId);
        }

        sessions.UnbindConnection(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    #endregion

    #region Battle Commands

    public Task Move(MoveRequest request)
    {
        var session = RequireSession();
        var participant = RequireParticipant(session);

        logger.LogDebug(
            "Move session {SessionId} player {PlayerEntityId} seq {ClientSequence} -> ({X}, {Y})",
            session.Id,
            participant.Player.Id.Value,
            request.ClientSequence,
            request.X,
            request.Y);

        session.World.Commands.Enqueue(
            new MoveCommand(participant.Player.Id, new Vector2(request.X, request.Y)));
        return Task.CompletedTask;
    }

    public Task UseSkill(UseSkillRequest request)
    {
        var session = RequireSession();
        var participant = RequireParticipant(session);
        var skill = participant.Player.FindSkill(request.SkillId)
            ?? throw new HubException($"Unknown skill '{request.SkillId}'.");

        logger.LogDebug(
            "UseSkill session {SessionId} player {PlayerEntityId} seq {ClientSequence} skill {SkillId} targetMode {TargetMode} targetEntity {TargetEntityId}",
            session.Id,
            participant.Player.Id.Value,
            request.ClientSequence,
            request.SkillId,
            request.Target?.Mode,
            request.Target?.EntityId);

        session.World.Commands.Enqueue(
            new UseSkillCommand(
                participant.Player.Id,
                skill,
                SkillTargetMapper.ToBattleTarget(request.Target)));
        return Task.CompletedTask;
    }

    #endregion

    #region Practice

    public async Task<PracticeSettingsDto> UpdatePracticeSettings(UpdatePracticeSettingsRequest request)
    {
        var session = RequireSession();
        if (session.Mode != RaidMode.Practice)
        {
            throw new HubException("Practice settings can only be changed in Practice mode.");
        }

        var practice = session.World.Settings.Practice;
        practice.Apply(request.HighManaRegen, request.IgnoreCooldowns);

        var settingsDto = BattleDtoMapper.ToPracticeSettingsDto(practice);
        await Clients.Group(session.GroupName).SendAsync(
            "SessionEvent",
            new SessionEventMessage(
                session.Id,
                session.Mode,
                new SessionEventDto(
                    SessionEventType.PracticeSettingsChanged,
                    PracticeSettings: settingsDto)));

        logger.LogInformation(
            "Session {SessionId} practice settings updated: highManaRegen={HighManaRegen}, ignoreCooldowns={IgnoreCooldowns}",
            session.Id,
            practice.HighManaRegen,
            practice.IgnoreCooldowns);

        return settingsDto;
    }

    #endregion

    #region Helpers

    private RaidSession RequireSession()
    {
        if (!sessions.TryGetSessionByConnection(Context.ConnectionId, out var session) || session is null)
        {
            throw new HubException("Join a session before sending commands.");
        }

        return session;
    }

    private SessionParticipant RequireParticipant(RaidSession session)
    {
        if (!session.TryGetByConnection(Context.ConnectionId, out var participant) || participant is null)
        {
            throw new HubException("Join a session before sending commands.");
        }

        return participant;
    }

    #endregion
}
