using System;
using UnityEngine.InputSystem;

namespace Raid.Player
{
    public sealed class RaidPlayerInput : IDisposable
    {
        private readonly InputActionMap _map;
        private readonly InputAction[] _skills = new InputAction[PlayerOptions.SkillSlotCount];
        private readonly InputAction _stop;
        private readonly InputAction _attackMove;
        private readonly InputAction _confirm;
        private readonly InputAction _commandMove;
        private bool _disposed;

        public event Action<int> SkillPressed;
        public event Action<int> SkillReleased;
        public event Action StopPressed;
        public event Action AttackMovePressed;

        public bool ConfirmWasPressedThisFrame => _confirm.WasPressedThisFrame();
        public bool ConfirmWasReleasedThisFrame => _confirm.WasReleasedThisFrame();
        public bool CommandMoveWasPressedThisFrame => _commandMove.WasPressedThisFrame();

        public RaidPlayerInput()
        {
            _map = new InputActionMap("RaidGameplay");

            for (var i = 0; i < _skills.Length; i++)
            {
                var index = i;
                var action = _map.AddAction($"Skill{i}", InputActionType.Button);
                action.AddBinding("<Keyboard>/q");
                action.performed += _ => SkillPressed?.Invoke(index);
                action.canceled += _ => SkillReleased?.Invoke(index);
                _skills[i] = action;
            }

            _stop = _map.AddAction("Stop", InputActionType.Button);
            _stop.AddBinding("<Keyboard>/s");
            _stop.performed += _ => StopPressed?.Invoke();

            _attackMove = _map.AddAction("AttackMove", InputActionType.Button);
            _attackMove.AddBinding("<Keyboard>/a");
            _attackMove.performed += _ => AttackMovePressed?.Invoke();

            _confirm = _map.AddAction("Confirm", InputActionType.Button);
            _confirm.AddBinding("<Mouse>/leftButton");

            _commandMove = _map.AddAction("CommandMove", InputActionType.Button);
            _commandMove.AddBinding("<Mouse>/rightButton");
        }

        public void ApplySkillBindings(PlayerOptions options)
        {
            for (var i = 0; i < _skills.Length; i++)
            {
                var path = ToBindingPath(options.GetSkillKey(i));
                if (string.IsNullOrEmpty(path))
                {
                    _skills[i].ApplyBindingOverride(0, string.Empty);
                    continue;
                }

                _skills[i].ApplyBindingOverride(0, path);
            }
        }

        public void Enable()
        {
            _map.Enable();
        }

        public void Disable()
        {
            _map.Disable();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _map.Disable();
            _map.Dispose();
        }

        public static string ToBindingPath(Key key)
        {
            if (key == Key.None)
            {
                return string.Empty;
            }

            try
            {
                var keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    var control = keyboard[key];
                    if (control != null)
                    {
                        return $"<Keyboard>/{control.name}";
                    }
                }
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            if (key is >= Key.A and <= Key.Z)
            {
                return $"<Keyboard>/{(char)('a' + (key - Key.A))}";
            }

            if (key is >= Key.Digit1 and <= Key.Digit9)
            {
                return $"<Keyboard>/{1 + (key - Key.Digit1)}";
            }

            if (key == Key.Digit0)
            {
                return "<Keyboard>/0";
            }

            return $"<Keyboard>/{key.ToString().ToLowerInvariant()}";
        }
    }
}
