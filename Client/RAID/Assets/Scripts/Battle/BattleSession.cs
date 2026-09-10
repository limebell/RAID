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
using Raid.Vfx;
using UnityEngine;

namespace Raid.Battle
{
    public class BattleSession : MonoBehaviour
    {
        public static BattleSession Instance { get; private set; }

        private const string BaseUrl = "http://localhost:5122"; //"http://118.44.45.220:5122";
        [SerializeField] private EntityRegistry _registry;
        [SerializeField] private CameraFollow _cameraFollow;
        [SerializeField] private PlayerInfoView _playerInfoView;
        [SerializeField] private TargetHealthView _targetHealthView;

        private readonly ConcurrentQueue<BattleTickMessage> _pendingTicks = new();

        private HubClient _client;
        private long _playerEntityId;
        private LocalPlayerState _localPlayerState = new();
        private SkillVfxDirector _vfx;

        public long PlayerEntityId => _playerEntityId;
        public HubClient Client => _client;
        public LocalPlayerState LocalPlayerState => _localPlayerState;
        public EntityRegistry Registry => _registry;

        private void Awake()
        {
            Instance = this;
            _vfx = gameObject.GetComponent<SkillVfxDirector>();
            _vfx.Bind(_registry);
        }

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
            _playerInfoView.Bind(_localPlayerState);
            _targetHealthView.Bind(_localPlayerState);

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

            _localPlayerState.Tick(Time.deltaTime);
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
                        break;
                    }

                    SnapView(moving, entity);
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

                    if (e.Type == BattleEventType.ActionStarted)
                    {
                        SnapView(view, entity);
                    }

                    view.ApplyActionStatus(entity.IsBusy, entity.CurrentPhase, e.SkillId);
                    if (entity.EntityId == _playerEntityId)
                    {
                        ApplyLocalActionGauge(e);
                    }

                    TryPlaySkillVfx(e, entity.EntityId);
                    break;
                }

                case BattleEventType.StatusEffectApplied:
                case BattleEventType.StatusEffectRemoved:
                {
                    if (entity.EntityId == _playerEntityId)
                    {
                        _localPlayerState.ApplyBuff(
                            e.Reason,
                            applied: e.Type == BattleEventType.StatusEffectApplied);
                    }

                    break;
                }

                case BattleEventType.DamageApplied:
                {
                    if (!_registry.TryGet(entity.EntityId, out _))
                    {
                        _registry.Spawn(entity);
                    }

                    if (entity.EntityId == _playerEntityId)
                    {
                        _localPlayerState.ApplyVitals(entity);
                    }

                    if (e.AttackerId == _playerEntityId &&
                        entity.EntityId != _playerEntityId)
                    {
                        _localPlayerState.RememberAttackedTarget(entity);
                    }
                    else
                    {
                        _localPlayerState.ApplyTargetVitalsIfTracked(entity);
                    }

                    if (e.AttackerId is long attackerId &&
                        e.Amount is float amount)
                    {
                        _vfx.ConfirmHit(
                            attackerId,
                            entity.EntityId,
                            e.SkillId ?? string.Empty,
                            amount);
                    }

                    break;
                }

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
                        _localPlayerState.ApplyVitals(entity);
                    }

                    break;
                }

                case BattleEventType.CooldownStarted:
                {
                    if (entity.EntityId == _playerEntityId &&
                        !string.IsNullOrEmpty(e.SkillId) &&
                        e.Amount is float duration)
                    {
                        _localPlayerState.StartCooldown(e.SkillId, duration);
                    }

                    break;
                }

                case BattleEventType.CooldownReady:
                {
                    if (entity.EntityId == _playerEntityId &&
                        !string.IsNullOrEmpty(e.SkillId))
                    {
                        _localPlayerState.ReadyCooldown(e.SkillId);
                    }

                    break;
                }
            }
        }

        private static void SnapView(EntityView view, EntitySnapshotDto entity)
        {
            var direction = new Vector2(entity.FacingDirection.X, entity.FacingDirection.Y);
            var position = new Vector2(entity.Position.X, entity.Position.Y);
            view.Engine.SnapTo(position, direction);
        }

        private void ApplyLocalActionGauge(BattleEventDto e)
        {
            if (e.Type == BattleEventType.ActionEnded)
            {
                _localPlayerState.StopActionGauge();
                return;
            }

            _localPlayerState.StartActionGauge(e.Phase, e.SkillId, e.Amount);
        }

        private void TryPlaySkillVfx(BattleEventDto e, long casterId)
        {
            if (e.Type != BattleEventType.ActionPhaseChanged ||
                !string.Equals(e.Phase, "Activation", System.StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrEmpty(e.SkillId) ||
                !_registry.TryGet(casterId, out var caster))
            {
                return;
            }

            EntityView target = null;
            var point = caster.transform.position;
            var direction = Vector3.zero;
            if (e.Target?.Direction is { } aim)
            {
                direction = new Vector3(aim.X, 0f, aim.Y);
            }
            else if (e.Target?.EntityId is long targetId &&
                _registry.TryGet(targetId, out var targetView))
            {
                target = targetView;
                point = targetView.transform.position;
            }
            else if (e.Target?.Position is { } position)
            {
                point = new Vector3(position.X, 0f, position.Y);
            }

            var speed = 0f;
            var radius = 0f;
            var range = 0f;
            if (_localPlayerState.TryGetSlotBySkillId(e.SkillId, out var slot))
            {
                speed = slot.ProjectileSpeed;
                radius = slot.Width;
                range = slot.Range;
            }

            _vfx.Play(e.SkillId, new SkillVfxContext(
                e.SkillId,
                caster,
                target,
                point,
                speed,
                radius,
                direction,
                range));
        }

        private async void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            _localPlayerState.Dispose();
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
