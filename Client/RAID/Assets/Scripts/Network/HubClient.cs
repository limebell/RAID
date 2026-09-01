using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Raid.Contracts.Battle;
using Raid.Contracts.Battle.Commands;
using Raid.Contracts.Common;
using UnityEngine;

namespace Raid.Network
{
    /// <summary>SignalR 전송만 담당합니다.</summary>
    public sealed class HubClient : IAsyncDisposable
    {
        private readonly string _baseUrl;
        private HubConnection _connection;
        private int _sequence;

        public bool IsInitialized => _connection != null;

        public bool IsConnected =>
            _connection?.State == HubConnectionState.Connected;

        public event Action<BattleTickMessage> BattleTickReceived;

        public HubClient(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        public void Initialize()
        {
            if (_connection != null)
            {
                throw new InvalidOperationException(
                    "HubClient is already initialized.");
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

        public async Task<JoinSessionResponse> ConnectAsync(
            RaidMode mode = RaidMode.Practice)
        {
            if (_connection == null)
            {
                throw new InvalidOperationException(
                    "HubClient is not initialized.");
            }

            if (_connection.State != HubConnectionState.Connected &&
                _connection.State != HubConnectionState.Connecting)
            {
                Debug.Log("Connecting to battle hub");
                await _connection.StartAsync();
            }

            var response = await _connection.InvokeAsync<JoinSessionResponse>(
                "JoinSession",
                new JoinSessionRequest(mode));

            if (response is null)
            {
                throw new InvalidOperationException("JoinSession returned null.");
            }

            Debug.Log(
                $"Joined session {response.SessionId}, " +
                $"player={response.PlayerEntityId}, slot={response.ParticipantSlot}");
            return response;
        }

        public async Task DisconnectAsync()
        {
            if (_connection == null ||
                _connection.State == HubConnectionState.Disconnected)
            {
                Debug.LogError($"Failed to disconnect: connection is null or already disconnected");
                return;
            }

            await _connection.StopAsync();
        }

        public void Move(Vector2 position)
        {
            if (_connection == null || !IsConnected)
            {
                Debug.LogError($"Failed to move: connection is null or not connected");
                return;
            }

            _ = _connection.InvokeAsync(
                "Move",
                new MoveRequest(_sequence++, position.x, position.y));
        }

        public void UseSkill(string skillId, SkillTargetDto target = null)
        {
            if (_connection == null || !IsConnected || string.IsNullOrEmpty(skillId))
            {
                Debug.LogError($"Failed to use skill {skillId}: connection is null or not connected");
                return;
            }

            _ = _connection.InvokeAsync(
                "UseSkill",
                new UseSkillRequest(_sequence++, skillId, target));
        }

        private void RegisterHandlers()
        {
            _connection.On<BattleTickMessage>("BattleTick", message =>
            {
                BattleTickReceived?.Invoke(message);
            });
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
}
