using Raid.Battle;
using Raid.Contracts.Battle.Commands;
using Raid.Contracts.Common;
using Raid.Entity;
using Raid.Network;
using Raid.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Raid.Player
{
    public class LocalInput : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private LayerMask _groundMask;
        [SerializeField] private LayerMask _entityMask;
        [SerializeField] private PlayerOptionsView _optionsView;
        [SerializeField] private ParticleSystem _moveIndicator;

        private SkillSlotState _pendingSkill;
        private SkillSlotState _heldSkill;
        private bool _attackMovePending;
        private bool _releaseHeldWithMouse;
        private bool _ignoreMouseReleaseThisFrame;
        private EntityView _hoveredEntity;
        private RaidPlayerInput _input;

        private BattleSession Session => BattleSession.Instance;
        private HubClient _client => Session != null ? Session.Client : null;

        private void OnEnable()
        {
            _input = new RaidPlayerInput();
            _input.ApplySkillBindings(PlayerOptions.Current);
            _input.SkillPressed += OnSkillPressed;
            _input.SkillReleased += OnSkillReleased;
            _input.StopPressed += OnStopPressed;
            _input.AttackMovePressed += OnAttackMovePressed;
            PlayerOptions.Current.SkillKeysChanged += ApplySkillBindings;
            _input.Enable();
        }

        private void OnDisable()
        {
            PlayerOptions.Current.SkillKeysChanged -= ApplySkillBindings;
            if (_input != null)
            {
                _input.SkillPressed -= OnSkillPressed;
                _input.SkillReleased -= OnSkillReleased;
                _input.StopPressed -= OnStopPressed;
                _input.AttackMovePressed -= OnAttackMovePressed;
                _input.Dispose();
                _input = null;
            }

            ReleaseHeldSkill();
            CancelAttackMovePending();
            ClearEntityHover();
        }

        private void ApplySkillBindings()
        {
            _input?.ApplySkillBindings(PlayerOptions.Current);
        }

        private void Update()
        {
            if (!CanHandleGameplayInput())
            {
                ClearEntityHover();
                return;
            }

            if (_input.ConfirmWasPressedThisFrame)
            {
                OnConfirmPressed();
            }

            if (_input.ConfirmWasReleasedThisFrame)
            {
                OnConfirmReleased();
            }

            if (_input.CommandMoveWasPressedThisFrame)
            {
                OnCommandMovePressed();
            }

            RefreshRangePreview();
            UpdatePendingSkillAim();
            UpdateEntityHover();
        }

        private bool CanHandleGameplayInput()
        {
            return _optionsView != null
                && !_optionsView.IsOpen
                && !_optionsView.IsRebinding
                && _client != null
                && _client.IsConnected;
        }

        private void OnSkillPressed(int slotIndex)
        {
            Debug.Log(PlayerOptions.Current.GetSkillKey(slotIndex));
            if (!CanHandleGameplayInput())
            {
                return;
            }

            var state = Session.LocalPlayerState;
            var slot = state.GetSlot(slotIndex);
            OnSkillKeyPressed(slot);
        }

        private void OnSkillReleased(int slotIndex)
        {
            if (_heldSkill == null || _releaseHeldWithMouse)
            {
                return;
            }

            var state = Session.LocalPlayerState;
            var slot = state.GetSlot(slotIndex);
            if (slot.SkillId == _heldSkill.SkillId)
            {
                ReleaseHeldSkill();
            }
        }

        private void OnStopPressed()
        {
            if (!CanHandleGameplayInput())
            {
                return;
            }

            CancelPendingSkill();
            CancelAttackMovePending();
            _client.StopMoving();
        }

        private void OnAttackMovePressed()
        {
            if (!CanHandleGameplayInput())
            {
                return;
            }

            if (_attackMovePending)
            {
                CancelAttackMovePending();
                return;
            }

            SetAttackMovePending();
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null
                && EventSystem.current.IsPointerOverGameObject();
        }

        private void OnConfirmPressed()
        {
            if (IsPointerOverUi())
            {
                return;
            }

            if (_pendingSkill != null)
            {
                TryHandlePendingConfirm();
                return;
            }

            TryConfirmAttackMove();
        }

        private void OnConfirmReleased()
        {
            if (!_releaseHeldWithMouse)
            {
                return;
            }

            if (_ignoreMouseReleaseThisFrame)
            {
                _ignoreMouseReleaseThisFrame = false;
                return;
            }

            ReleaseHeldSkill();
        }

        private void OnCommandMovePressed()
        {
            if (IsPointerOverUi())
            {
                return;
            }

            CancelPendingSkill();
            CancelAttackMovePending();
            TryMoveToMousePosition();
        }

        private void ShowMoveIndicator(Vector3 position)
        {
            _moveIndicator.transform.position = position + Vector3.up * 0.02f;

            _moveIndicator.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _moveIndicator.Play();
        }

        private void ReleaseHeldSkill()
        {
            if (_heldSkill == null)
            {
                return;
            }

            var skillId = _heldSkill.SkillId;
            _heldSkill = null;
            _releaseHeldWithMouse = false;
            _ignoreMouseReleaseThisFrame = false;
            if (_client != null && _client.IsConnected)
            {
                _client.ReleaseSkill(skillId);
            }
        }

        private void OnSkillKeyPressed(SkillSlotState slot)
        {
            if (!slot.IsEnabled.Value || slot.RemainingCooldown.Value > 0f)
            {
                return;
            }

            CancelAttackMovePending();

            if (PlayerOptions.Current.SmartCasting.Value ||
                slot.TargetingMode == SkillTargetingMode.None)
            {
                CancelPendingSkill();
                if (!TryUseSkill(slot))
                {
                    Debug.LogWarning(
                        $"Could not use skill '{slot.SkillId}' ({slot.TargetingMode}).");
                }

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
            CancelAttackMovePending();
            _pendingSkill = slot;
            if (Session.Registry.TryGet(Session.PlayerEntityId, out var player))
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

            if (!Session.Registry.TryGet(Session.PlayerEntityId, out var player))
            {
                return;
            }

            player.UpdateSkillRangeAim(new Vector3(point.x, 0f, point.y));
        }

        private void SetAttackMovePending()
        {
            CancelPendingSkill();
            _attackMovePending = true;
            var slot = Session.LocalPlayerState.BasicAttack;
            if (slot == null ||
                !Session.Registry.TryGet(Session.PlayerEntityId, out var player))
            {
                return;
            }

            player.ShowSkillRange(SkillTargetingMode.Entity, slot.Range, slot.Width);
        }

        private void CancelAttackMovePending()
        {
            if (!_attackMovePending)
            {
                return;
            }

            _attackMovePending = false;
            if (_pendingSkill != null)
            {
                return;
            }

            if (Session.Registry.TryGet(Session.PlayerEntityId, out var player))
            {
                player.HideSkillRange();
            }
        }

        private void TryConfirmAttackMove()
        {
            if (!_attackMovePending)
            {
                return;
            }

            CancelAttackMovePending();

            if (TryResolveHostileEntity(out var entityId))
            {
                var slot = Session.LocalPlayerState.BasicAttack;
                if (slot != null)
                {
                    _client.UseSkill(
                        slot.SkillId,
                        new SkillTargetDto(SkillTargetingMode.Entity, EntityId: entityId));
                }

                return;
            }

            if (!TryResolveGroundPoint(out var point))
            {
                SetAttackMovePending();
                return;
            }

            ShowMoveIndicator(new Vector3(point.x, 0f, point.y));
            _client.AttackMove(point);
        }

        private void TryHandlePendingConfirm()
        {
            if (_pendingSkill == null)
            {
                return;
            }

            var slot = _pendingSkill;
            CancelPendingSkill();
            if (!TryUseSkill(slot, releaseWithMouse: slot.HoldsUntilRelease))
            {
                SetPendingSkill(slot);
            }
        }

        private void CancelPendingSkill()
        {
            _pendingSkill = null;
            if (Session.Registry.TryGet(Session.PlayerEntityId, out var player))
            {
                player.HideSkillRange();
            }
        }

        private bool TryUseSkill(SkillSlotState slot, bool releaseWithMouse = false)
        {
            if (!TryBuildTarget(slot.TargetingMode, out var target))
            {
                return false;
            }

            _client.UseSkill(slot.SkillId, target);
            if (slot.HoldsUntilRelease)
            {
                _heldSkill = slot;
                _releaseHeldWithMouse = releaseWithMouse;
                _ignoreMouseReleaseThisFrame = releaseWithMouse;
            }

            return true;
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
                        return false;
                    }

                    target = new SkillTargetDto(SkillTargetingMode.Entity, EntityId: entityId);
                    return true;
                }

                case SkillTargetingMode.Point:
                {
                    if (!TryResolveGroundPoint(out var point))
                    {
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
                        Debug.LogWarning("Direction-targeted skill needs a facing or ground point.");
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
            if (!TryGetEntityUnderCursor(out var view) ||
                view.EntityId == Session.PlayerEntityId)
            {
                return false;
            }

            entityId = view.EntityId;
            return true;
        }

        private bool TryResolveHostileEntity(out long entityId)
        {
            entityId = 0;
            if (!TryGetEntityUnderCursor(out var view) ||
                view.EntityId == Session.PlayerEntityId ||
                view.IsAlly)
            {
                return false;
            }

            entityId = view.EntityId;
            return true;
        }

        private void UpdateEntityHover()
        {
            if (!TryGetEntityUnderCursor(out var view))
            {
                ClearEntityHover();
                return;
            }

            if (_hoveredEntity == view)
            {
                return;
            }

            ClearEntityHover();
            _hoveredEntity = view;
            _hoveredEntity.SetHoverOutline(true);
        }

        private void ClearEntityHover()
        {
            if (_hoveredEntity == null)
            {
                return;
            }

            _hoveredEntity.SetHoverOutline(false);
            _hoveredEntity = null;
        }

        private bool TryGetEntityUnderCursor(out EntityView view)
        {
            view = null;

            if (Mouse.current == null)
            {
                return false;
            }

            var ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out var hit, Mathf.Infinity, _entityMask, QueryTriggerInteraction.Collide))
            {
                return false;
            }

            view = hit.collider.GetComponentInParent<EntityView>();
            return view != null;
        }

        private bool TryResolveGroundPoint(out Vector2 point)
        {
            point = default;

            if (Mouse.current == null)
            {
                return false;
            }

            var ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out var hit, Mathf.Infinity, _groundMask))
            {
                return false;
            }

            point = new Vector2(hit.point.x, hit.point.z);
            return true;
        }

        private void RefreshRangePreview()
        {
            if (!Session.Registry.TryGet(Session.PlayerEntityId, out var player))
            {
                return;
            }

            if (_pendingSkill != null)
            {
                _pendingSkill = Session.LocalPlayerState.Resolve(_pendingSkill);
                player.ShowSkillRange(
                    _pendingSkill.TargetingMode,
                    _pendingSkill.Range,
                    _pendingSkill.Width);
                return;
            }

            if (!_attackMovePending)
            {
                return;
            }

            var slot = Session.LocalPlayerState.BasicAttack;
            if (slot == null)
            {
                return;
            }

            player.ShowSkillRange(SkillTargetingMode.Entity, slot.Range, slot.Width);
        }

        private bool TryResolveDirection(out Vector2 direction)
        {
            direction = default;

            if (!Session.Registry.TryGet(Session.PlayerEntityId, out var player) ||
                player == null)
            {
                return false;
            }

            var origin = new Vector2(player.transform.position.x, player.transform.position.z);
            if (TryResolveGroundPoint(out var point))
            {
                direction = point - origin;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    direction.Normalize();
                    return true;
                }
            }

            var forward = player.transform.forward;
            direction = new Vector2(forward.x, forward.z);
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

            ShowMoveIndicator(new Vector3(point.x, 0f, point.y));
            _client.Move(point);
        }
    }
}
