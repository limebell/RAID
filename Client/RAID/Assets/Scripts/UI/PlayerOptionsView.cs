using System;
using R3;
using Raid.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Raid.UI
{
    public class PlayerOptionsView : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Toggle _smartCastToggle;
        [SerializeField] private Button[] _skillKeyButtons = new Button[PlayerOptions.SkillSlotCount];
        [SerializeField] private Button _resetDefaultsButton;
        [SerializeField] private Button _closeButton;

        private PlayerOptions _options;
        private DisposableBag _subscriptions = new();
        private int _rebindSlot = -1;
        private bool _isOpen;

        public bool IsOpen => _isOpen;
        public bool IsRebinding => _rebindSlot >= 0;

        private void Start()
        {
            AddListeners();
            Bind(PlayerOptions.Current);
        }

        private void AddListeners()
        {
            _smartCastToggle.onValueChanged.AddListener(value =>
            {
                _options.SmartCasting.Value = value;
            });

            for (var i = 0; i < PlayerOptions.SkillSlotCount; i++)
            {
                var slot = i;
                _skillKeyButtons[i].onClick.AddListener(() => BeginRebind(slot));
            }

            _resetDefaultsButton.onClick.AddListener(() =>
            {
                _options.ResetToDefaults();
                RefreshKeyLabels();
                CancelRebind();
            });

            _closeButton.onClick.AddListener(() => SetOpen(false));
        }

        private void Bind(PlayerOptions options)
        {
            _subscriptions.Dispose();
            _subscriptions = new();
            _options = options;

            _options.SmartCasting
                .Subscribe(value =>
                {
                    if (_smartCastToggle.isOn != value)
                    {
                        _smartCastToggle.SetIsOnWithoutNotify(value);
                    }
                })
                .AddTo(ref _subscriptions);

            RefreshKeyLabels();
            SetOpen(false);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (_rebindSlot >= 0)
            {
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    CancelRebind();
                    return;
                }

                if (TryReadPressedKey(out var key))
                {
                    _options.SetSkillKey(_rebindSlot, key);
                    _rebindSlot = -1;
                    RefreshKeyLabels();
                }

                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                SetOpen(!_isOpen);
            }
        }

        private void BeginRebind(int slot)
        {
            _rebindSlot = slot;
            GetSkillKeyLabel(slot).text = "...";
        }

        private void CancelRebind()
        {
            _rebindSlot = -1;
            RefreshKeyLabels();
        }

        private void RefreshKeyLabels()
        {
            for (var i = 0; i < PlayerOptions.SkillSlotCount; i++)
            {
                GetSkillKeyLabel(i).text = _options.GetSkillKey(i).ToString();
            }
        }

        private TMP_Text GetSkillKeyLabel(int slotIndex) =>
            _skillKeyButtons[slotIndex].GetComponentInChildren<TMP_Text>(true);

        public void SetOpen(bool open)
        {
            _isOpen = open;
            CancelRebind();
            _panel.SetActive(open);
        }

        private static bool TryReadPressedKey(out Key key)
        {
            key = Key.None;
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            foreach (Key value in Enum.GetValues(typeof(Key)))
            {
                if (value is Key.None or Key.Escape)
                {
                    continue;
                }

                try
                {
                    if (keyboard[value].wasPressedThisFrame)
                    {
                        key = value;
                        return true;
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Input System Key enum에 유효하지 않은 값이 포함될 수 있음
                }
            }

            return false;
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
        }
    }
}
