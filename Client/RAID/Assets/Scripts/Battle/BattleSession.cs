using System.Collections.Concurrent;
using System.Linq;
using Raid.Cameras;
using Raid.Contracts.Battle;
using Raid.Contracts.Battle.Events;
using Raid.Contracts.Battle.Snapshots;
using Raid.Entity;
using Raid.Network;
using Raid.Player;
using Raid.UI;
using UnityEngine;

namespace Raid.Battle
{
    public class BattleSession : MonoBehaviour
    {
        private const string BaseUrl = "http://localhost:5122"; //"http://118.44.45.220:5122";
        [SerializeField] private EntityRegistry _registry;
        [SerializeField] private CameraFollow _cameraFollow;
        [SerializeField] private PlayerInfoView _playerInfoView;

        private readonly ConcurrentQueue<BattleTickMessage> _pendingTicks = new();

        private HubClient _client;
        private long _playerEntityId;
        private LocalPlayerState _localPlayerState;

        public long PlayerEntityId => _playerEntityId;
        public HubClient Client => _client;
        public LocalPlayerState LocalPlayerState => _localPlayerState;
        public EntityRegistry Registry => _registry;

        private async void Start()
        {
            _client = new HubClient(BaseUrl);
            _client.Initialize();
            _client.BattleTickReceived += OnBattleTickReceived;

            try
            {
                JoinSessionResponse join = await _client.ConnectAsync();
                _playerEntityId = join.PlayerEntityId;

                foreach (var entity in join.Snapshot.Entities)
                {
                    _registry.Spawn(entity);
                }

                var localEntity = join.Snapshot.Entities.FirstOrDefault(e => e.EntityId == _playerEntityId);
                _localPlayerState?.Dispose();
                _localPlayerState = new LocalPlayerState();
                _localPlayerState.InitializeFromJoin(localEntity, join.Skills);

                BindLocalPlayer();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to join battle session: {ex}");
            }
        }

        private void BindLocalPlayer()
        {
            _playerInfoView?.Bind(_localPlayerState);

            if (_registry.TryGet(_playerEntityId, out var player))
            {
                _cameraFollow.Bind(player.transform);
            }
            else
            {
                Debug.LogWarning(
                    $"Local player entity {_playerEntityId} not found in snapshot.");
            }
        }

        private void OnBattleTickReceived(BattleTickMessage message)
        {
            _pendingTicks.Enqueue(message);
        }

        private void Update()
        {
            while (_pendingTicks.TryDequeue(out var tick))
            {
                ApplyTick(tick);
            }

            _localPlayerState?.Tick(Time.deltaTime);
        }

        private void ApplyTick(BattleTickMessage message)
        {
            foreach (var e in message.Events)
            {
                ApplyEvent(e);
            }
        }

        private void ApplyEvent(BattleEventDto e)
        {
            if (e.Entity is not EntitySnapshotDto entity)
            {
                return;
            }

            switch (e.Type)
            {
                case BattleEventType.Unknown:
                    break;
                case BattleEventType.EntityMoved:
                case BattleEventType.EntityMoveCompleted:
                {
                    if (!_registry.TryGet(entity.EntityId, out var moving))
                    {
                        Debug.LogWarning($"Event {e.Type} for Entity {entity.EntityId} not found in registry.");
                        break;
                    }

                    var direction = new Vector2(entity.FacingDirection.X, entity.FacingDirection.Y);
                    var position = new Vector2(entity.Position.X, entity.Position.Y);
                    moving.Engine.PushSnapshot(position, direction);

                    if (entity.IsBusy)
                    {
                        moving.ApplyActionStatus(entity.IsBusy, entity.CurrentPhase, e.SkillId);
                    }
                    else if (e.Type == BattleEventType.EntityMoved)
                    {
                        moving.SetActionStatus(EntityActionStatus.Moving, null);
                    }
                    else
                    {
                        moving.SetActionStatus(EntityActionStatus.Idle, null);
                    }

                    break;
                }

                case BattleEventType.PositionSet:
                {
                    if (!_registry.TryGet(entity.EntityId, out var moving))
                    {
                        Debug.LogWarning($"Event {e.Type} for Entity {entity.EntityId} not found in registry.");
                    }

                    var direction = new Vector2(entity.FacingDirection.X, entity.FacingDirection.Y);
                    var position = new Vector2(entity.Position.X, entity.Position.Y);
                    moving?.Engine.SnapTo(position, direction);
                    break;
                }

                case BattleEventType.ActionStarted:
                case BattleEventType.ActionPhaseChanged:
                case BattleEventType.ActionEnded:
                {
                    if (!_registry.TryGet(entity.EntityId, out var view))
                    {
                        Debug.LogWarning($"Event {e.Type} for Entity {entity.EntityId} not found in registry.");
                        break;
                    }

                    view.ApplyActionStatus(entity.IsBusy, entity.CurrentPhase, e.SkillId);
                    break;
                }

                case BattleEventType.DamageApplied:
                case BattleEventType.EntitySpawned:
                {
                    if (!_registry.TryGet(entity.EntityId, out _))
                    {
                        _registry.Spawn(entity);
                    }
                    break;
                }

                case BattleEventType.ResourceChanged:
                {
                    if (entity.EntityId == _playerEntityId)
                    {
                        _localPlayerState?.ApplyVitals(entity);
                    }

                    break;
                }

                case BattleEventType.CooldownStarted:
                {
                    if (entity.EntityId == _playerEntityId &&
                        !string.IsNullOrEmpty(e.SkillId) &&
                        e.Amount is float duration)
                    {
                        _localPlayerState?.StartCooldown(e.SkillId, duration);
                    }

                    break;
                }

                case BattleEventType.CooldownReady:
                {
                    if (entity.EntityId == _playerEntityId &&
                        !string.IsNullOrEmpty(e.SkillId))
                    {
                        _localPlayerState?.ReadyCooldown(e.SkillId);
                    }

                    break;
                }
            }
        }

        private async void OnDestroy()
        {
            _localPlayerState?.Dispose();
            _localPlayerState = null;

            if (_client == null)
            {
                return;
            }

            _client.BattleTickReceived -= OnBattleTickReceived;
            await _client.DisconnectAsync();
            await _client.DisposeAsync();
        }
    }
}
