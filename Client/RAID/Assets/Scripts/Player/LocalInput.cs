using Raid.Battle;
using Raid.Contracts.Battle.Commands;
using Raid.Contracts.Common;
using Raid.Entity;
using Raid.Network;
using Raid.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Raid.Player
{
    public class LocalInput : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private LayerMask _groundMask;
        [SerializeField] private PlayerOptionsView _optionsView;

        private BattleSession _battleSession;
        private SkillSlotState _pendingSkill;

        private HubClient _client => _battleSession != null ? _battleSession.Client : null;

        private void Start()
        {
            _battleSession = GetComponent<BattleSession>();
            if (_optionsView == null)
            {
                _optionsView = FindFirstObjectByType<PlayerOptionsView>();
            }
        }

        private void Update()
        {
            if (_optionsView != null && (_optionsView.IsOpen || _optionsView.IsRebinding))
            {
                return;
            }

            if (_client == null || !_client.IsConnected)
            {
                return;
            }

            TryHandleSkillKeys();
            TryHandlePendingConfirm();
            UpdatePendingSkillAim();

            if (_camera == null || Mouse.current == null)
            {
                return;
            }

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                if (_pendingSkill != null)
                {
                    CancelPendingSkill();
                }

                TryMoveToMousePosition();
            }
        }

        private void TryHandleSkillKeys()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            var state = _battleSession.LocalPlayerState;
            if (state == null)
            {
                return;
            }

            var options = PlayerOptions.Current;
            for (var i = 0; i < PlayerOptions.SkillSlotCount; i++)
            {
                var key = options.GetSkillKey(i);
                if (key == Key.None || !keyboard[key].wasPressedThisFrame)
                {
                    continue;
                }

                if (!state.TryGetSlot(i, out var slot))
                {
                    continue;
                }

                OnSkillKeyPressed(slot);
                break;
            }
        }

        private void OnSkillKeyPressed(SkillSlotState slot)
        {
            if (!slot.IsEnabled.Value || slot.RemainingCooldown.Value > 0f)
            {
                return;
            }

            if (PlayerOptions.Current.SmartCasting.Value ||
                slot.TargetingMode == SkillTargetingMode.None)
            {
                CancelPendingSkill();
                TryUseSkill(slot);
                return;
            }

            if (_pendingSkill != null && _pendingSkill.SkillId == slot.SkillId)
            {
                CancelPendingSkill();
                return;
            }

            SetPendingSkill(slot);
        }

        private void SetPendingSkill(SkillSlotState slot)
        {
            _pendingSkill = slot;
            if (_battleSession.Registry.TryGet(_battleSession.PlayerEntityId, out var player))
            {
                player.ShowSkillRange(slot.TargetingMode, slot.Range, slot.Width);
            }

            UpdatePendingSkillAim();    
        }

        private void UpdatePendingSkillAim()
        {
            if (_pendingSkill == null ||
                _pendingSkill.TargetingMode == SkillTargetingMode.Entity ||
                _pendingSkill.TargetingMode == SkillTargetingMode.None)
            {
                return;
            }

            if (!TryResolveGroundPoint(out var point))
            {
                return;
            }

            if (!_battleSession.Registry.TryGet(_battleSession.PlayerEntityId, out var player))
            {
                return;
            }

            player.UpdateSkillRangeAim(new Vector3(point.x, 0f, point.y));
        }

        private void TryHandlePendingConfirm()
        {
            if (_pendingSkill == null || Mouse.current == null)
            {
                return;
            }

            if (!Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            var slot = _pendingSkill;
            CancelPendingSkill();
            TryUseSkill(slot);
        }

        private void CancelPendingSkill()
        {
            _pendingSkill = null;
            if (_battleSession != null &&
                _battleSession.Registry.TryGet(_battleSession.PlayerEntityId, out var player))
            {
                player.HideSkillRange();
            }
        }

        private void TryUseSkill(SkillSlotState slot)
        {
            if (!TryBuildTarget(slot.TargetingMode, out var target))
            {
                return;
            }

            _client.UseSkill(slot.SkillId, target);
        }

        private bool TryBuildTarget(SkillTargetingMode mode, out SkillTargetDto target)
        {
            target = null;

            switch (mode)
            {
                case SkillTargetingMode.None:
                    target = new SkillTargetDto(SkillTargetingMode.None);
                    return true;

                case SkillTargetingMode.Entity:
                {
                    if (!TryResolveEntityTarget(out var entityId))
                    {
                        Debug.LogWarning("Entity-targeted skill needs an entity under the cursor.");
                        return false;
                    }

                    target = new SkillTargetDto(SkillTargetingMode.Entity, EntityId: entityId);
                    return true;
                }

                case SkillTargetingMode.Point:
                {
                    if (!TryResolveGroundPoint(out var point))
                    {
                        Debug.LogWarning("Point-targeted skill needs a ground point under the cursor.");
                        return false;
                    }

                    target = new SkillTargetDto(
                        SkillTargetingMode.Point,
                        Position: new Vector2Dto(point.x, point.y));
                    return true;
                }

                case SkillTargetingMode.Direction:
                {
                    if (!TryResolveDirection(out var direction))
                    {
                        Debug.LogWarning("Direction-targeted skill needs a ground point under the cursor.");
                        return false;
                    }

                    target = new SkillTargetDto(
                        SkillTargetingMode.Direction,
                        Direction: new Vector2Dto(direction.x, direction.y));
                    return true;
                }

                default:
                    Debug.LogWarning($"Unsupported targeting mode: {mode}");
                    return false;
            }
        }

        private bool TryResolveEntityTarget(out long entityId)
        {
            entityId = 0;

            if (_camera == null || Mouse.current == null)
            {
                return false;
            }

            var ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out var hit))
            {
                return false;
            }

            var view = hit.collider.GetComponentInParent<EntityView>();
            if (view == null || view.EntityId == _battleSession.PlayerEntityId)
            {
                return false;
            }

            entityId = view.EntityId;
            return true;
        }

        private bool TryResolveGroundPoint(out Vector2 point)
        {
            point = default;

            if (_camera == null || Mouse.current == null)
            {
                return false;
            }

            var ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            // Ground 레이어만 맞추면 엔티티 위 클릭도 바닥 좌표로 통과한다.
            if (!Physics.Raycast(ray, out var hit, Mathf.Infinity, _groundMask))
            {
                return false;
            }

            point = new Vector2(hit.point.x, hit.point.z);
            return true;
        }

        private bool TryResolveDirection(out Vector2 direction)
        {
            direction = default;

            if (!TryResolveGroundPoint(out var point))
            {
                return false;
            }

            if (!_battleSession.Registry.TryGet(_battleSession.PlayerEntityId, out var player) ||
                player == null)
            {
                return false;
            }

            var origin = new Vector2(player.transform.position.x, player.transform.position.z);
            direction = point - origin;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            direction.Normalize();
            return true;
        }

        private void TryMoveToMousePosition()
        {
            if (!TryResolveGroundPoint(out var point))
            {
                return;
            }

            _client.Move(point);
        }
    }
}
