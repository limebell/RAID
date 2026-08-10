using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using UnityEngine;
using Raid.Contracts.Battle;
using Raid.Contracts.Battle.Events;
using Raid.Contracts.Common;
using Raid.Contracts.Battle.Commands;

public sealed class HubClient : IAsyncDisposable
{
    private readonly string _baseUrl;
    private HubConnection _connection;

    private int _sequence = 0;

    public bool IsInitialized => _connection != null;

    public bool IsConnected =>
        _connection?.State == HubConnectionState.Connected;

    public HubClient(string baseUrl)
    {
        _baseUrl = baseUrl;
    }

    public EventHandler<BattleEventDto> OnBattleEvent;

    public void Initialize()
    {
        if (_connection != null)
        {
            throw new InvalidOperationException(
                "GameHubClient is already initialized.");
        }

        Debug.Log($"Initializing battle hub at {_baseUrl}/hubs/battle");
        _connection = new HubConnectionBuilder()
            .WithUrl(
                $"{_baseUrl}/hubs/battle",
                options =>
                {
                    options.Headers.Add("Client-Version", Application.version);
                })
            .WithAutomaticReconnect()
            .Build();

        RegisterHandlers();
    }

    public async Task ConnectAsync()
    {
        if (_connection == null)
        {
            throw new InvalidOperationException(
                "HubClient is not initialized.");
        }

        if (_connection.State == HubConnectionState.Connected ||
            _connection.State == HubConnectionState.Connecting)
        {
            return;
        }

        Debug.Log("Connecting to battle hub");
        await _connection.StartAsync();
        var response = await _connection.InvokeAsync<JoinSessionResponse>(
            "JoinSession",
            new JoinSessionRequest(RaidMode.Practice));
        Debug.Log($"Joined session: {response}");
        if (response is not null)
        {
            Debug.Log($"Session joined successfully: {response.SessionId}");
            Debug.Log($"Player entity id: {response.PlayerEntityId}");
            Debug.Log($"Participant slot: {response.ParticipantSlot}");
            Debug.Log($"Class id: {response.ClassId}");
            Debug.Log($"Skill ids: {string.Join(", ", response.SkillIds)}");
            Debug.Log($"Snapshot: {response.Snapshot}");
        }
        else
        {
            Debug.Log($"Failed to join session: {response}");
        }
    }

    public async Task DisconnectAsync()
    {
        if (_connection == null)
        {
            return;
        }

        if (_connection.State == HubConnectionState.Disconnected)
        {
            return;
        }

        await _connection.StopAsync();
    }

    private void RegisterHandlers()
    {
        _connection.On<BattleTickMessage>("BattleTick", HandleBattleTick);
    }

    private void HandleBattleTick(BattleTickMessage message)
    {
        foreach (var e in message.Events)
        {
            OnBattleEvent?.Invoke(this, e);
        }
    }

    public void Move(Vector2 position)
    {
        _ = _connection!.InvokeAsync(
            "Move",
            new MoveRequest(_sequence++, position.x, position.y));
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection == null)
        {
            return;
        }

        var connection = _connection;
        _connection = null;

        if (connection.State != HubConnectionState.Disconnected)
        {
            await connection.StopAsync();
        }

        await connection.DisposeAsync();
    }
}
