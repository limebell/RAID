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
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        RaidSession session;
        if (request.SessionId is Guid sessionId)
        {
            session = sessions.Find(sessionId)
                ?? throw new HubException($"Session '{sessionId}' was not found.");
            if (session.IsClosed)
            {
                throw new HubException($"Session '{sessionId}' is closed.");
            }
        }
        else
        {
            if (!Enum.IsDefined(request.Mode))
            {
                throw new HubException("JoinSession requires a valid Mode (Practice or Raid).");
            }

            session = sessions.Create(request.Mode);
            logger.LogInformation(
                "Created session {SessionId} mode {Mode} for user {UserId}",
                session.Id,
                session.Mode,
                request.UserId);
        }

        SessionParticipant participant;
        try
        {
            participant = session.Join(request.UserId);
        }
        catch (InvalidOperationException) when (session.IsClosed)
        {
            throw new HubException($"Session '{session.Id}' is closed.");
        }
        catch (InvalidOperationException exception)
        {
            throw new HubException(exception.Message);
        }

        sessions.Bind(Context.ConnectionId, request.UserId, session.Id);
        await Groups.AddToGroupAsync(Context.ConnectionId, session.GroupName);

        logger.LogInformation(
            "User {UserId} joined session {SessionId} as player {PlayerEntityId} slot {Slot}",
            request.UserId,
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
            playerClass.Skills
                .Select(skill => new SkillInfoDto(
                    skill.SkillId,
                    skill.TargetingMode,
                    skill.ManaCost,
                    skill.Range,
                    skill.Width))
                .ToArray(),
            BattleDtoMapper.ToPracticeSettingsDto(session.World.Settings.Practice),
            BattleDtoMapper.ToSnapshot(session));
    }

    public BattleSnapshotDto RequestSnapshot()
    {
        var (session, _) = RequireCaller();
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
        var (session, participant) = RequireCaller();

        logger.LogTrace(
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

    public Task StopMoving(StopMovingRequest request)
    {
        var (session, participant) = RequireCaller();

        logger.LogTrace(
            "StopMoving session {SessionId} player {PlayerEntityId} seq {ClientSequence}",
            session.Id,
            participant.Player.Id.Value,
            request.ClientSequence);

        session.World.Commands.Enqueue(
            new StopMovingCommand(participant.Player.Id));
        return Task.CompletedTask;
    }

    public Task UseSkill(UseSkillRequest request)
    {
        var (session, participant) = RequireCaller();
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
        var (session, _) = RequireCaller();
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

    private (RaidSession Session, SessionParticipant Participant) RequireCaller()
    {
        if (!sessions.TryGetByConnection(Context.ConnectionId, out var session, out var userId)
            || session is null
            || userId is null)
        {
            throw new HubException("Join a session before sending commands.");
        }

        if (session.IsClosed)
        {
            throw new HubException($"Session '{session.Id}' is closed.");
        }

        if (!session.TryGetByUserId(userId, out var participant)
            || participant is null
            || !participant.IsConnected)
        {
            throw new HubException("Join a session before sending commands.");
        }

        return (session, participant);
    }

    #endregion
}
