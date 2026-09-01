using System;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Raid.Player
{
    public sealed class PlayerOptions
    {
        public const int SkillSlotCount = 6;

        private const string SmartCastKey = "raid.options.smartCast";
        private const string SkillKeyPrefix = "raid.options.skillKey.";

        private static readonly Key[] DefaultSkillKeys =
        {
            Key.Q, Key.W, Key.E, Key.R, Key.D, Key.F
        };

        private static PlayerOptions _current;

        private readonly Key[] _skillKeys = new Key[SkillSlotCount];
        private DisposableBag _saveSubscriptions = new();

        public static PlayerOptions Current => _current ??= Load();

        public ReactiveProperty<bool> SmartCasting { get; }

        private PlayerOptions(bool smartCasting, Key[] skillKeys)
        {
            SmartCasting = new ReactiveProperty<bool>(smartCasting);
            Array.Copy(skillKeys, _skillKeys, SkillSlotCount);
            SmartCasting.Subscribe(_ => Save()).AddTo(ref _saveSubscriptions);
        }

        private static PlayerOptions Load()
        {
            var smartCasting = PlayerPrefs.GetInt(SmartCastKey, 1) != 0;
            var keys = new Key[SkillSlotCount];
            for (var i = 0; i < SkillSlotCount; i++)
            {
                var raw = PlayerPrefs.GetInt(SkillKeyPrefix + i, (int)DefaultSkillKeys[i]);
                keys[i] = Enum.IsDefined(typeof(Key), raw) ? (Key)raw : DefaultSkillKeys[i];
            }

            return new PlayerOptions(smartCasting, keys);
        }

        public Key GetSkillKey(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SkillSlotCount)
            {
                return Key.None;
            }

            return _skillKeys[slotIndex];
        }

        public void SetSkillKey(int slotIndex, Key key)
        {
            if (slotIndex < 0 || slotIndex >= SkillSlotCount || key == Key.None)
            {
                return;
            }

            for (var i = 0; i < SkillSlotCount; i++)
            {
                if (i != slotIndex && _skillKeys[i] == key)
                {
                    _skillKeys[i] = _skillKeys[slotIndex];
                    break;
                }
            }

            _skillKeys[slotIndex] = key;
            Save();
        }

        public void ResetToDefaults()
        {
            SmartCasting.Value = true;
            Array.Copy(DefaultSkillKeys, _skillKeys, SkillSlotCount);
            Save();
        }

        public void Save()
        {
            PlayerPrefs.SetInt(SmartCastKey, SmartCasting.Value ? 1 : 0);
            for (var i = 0; i < SkillSlotCount; i++)
            {
                PlayerPrefs.SetInt(SkillKeyPrefix + i, (int)_skillKeys[i]);
            }

            PlayerPrefs.Save();
        }
    }
}
