using System;
using System.Collections.Generic;
using R3;
using Raid.Contracts.Battle;
using Raid.Contracts.Battle.Snapshots;
using Raid.Contracts.Common;

namespace Raid.Player
{
    public sealed class LocalPlayerState : IDisposable
    {
        private readonly Dictionary<string, SkillSlotState> _slotsById = new();
        private readonly List<SkillSlotState> _slots = new();
        private bool _disposed;

        public ReactiveProperty<float> CurrentHealth { get; } = new(0f);
        public ReactiveProperty<float> MaxHealth { get; } = new(0f);
        public ReactiveProperty<float> CurrentMana { get; } = new(0f);
        public ReactiveProperty<float> MaxMana { get; } = new(0f);

        public IReadOnlyList<SkillSlotState> Slots => _slots;

        public bool TryGetSlot(int slotIndex, out SkillSlotState slot)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count)
            {
                slot = null;
                return false;
            }

            slot = _slots[slotIndex];
            return slot != null && !string.IsNullOrEmpty(slot.SkillId);
        }

        public void InitializeFromJoin(EntitySnapshotDto entity, IReadOnlyList<SkillInfoDto> skills)
        {
            ApplyVitals(entity);
            SetSkills(skills);
        }

        public void ApplyVitals(EntitySnapshotDto entity)
        {
            CurrentHealth.Value = entity.CurrentHealth;
            MaxHealth.Value = entity.MaxHealth;
            CurrentMana.Value = entity.CurrentMana;
            MaxMana.Value = entity.MaxMana;
        }

        public void SetSkills(IReadOnlyList<SkillInfoDto> skills)
        {
            ClearSlots();

            if (skills == null)
            {
                return;
            }

            foreach (var skill in skills)
            {
                if (skill == null ||
                    string.IsNullOrEmpty(skill.SkillId) ||
                    _slotsById.ContainsKey(skill.SkillId))
                {
                    continue;
                }

                var slot = new SkillSlotState(
                    skill.SkillId,
                    skill.TargetingMode,
                    skill.ManaCost,
                    skill.Range,
                    skill.Width);
                _slots.Add(slot);
                _slotsById[skill.SkillId] = slot;
            }
        }

        public void DisableSkill(string skillId)
        {
            if (!_slotsById.TryGetValue(skillId, out var slot))
            {
                return;
            }

            slot.IsEnabled.Value = false;
        }

        public void StartCooldown(string skillId, float durationSeconds)
        {
            if (!_slotsById.TryGetValue(skillId, out var slot))
            {
                return;
            }

            slot.CooldownDuration.Value = Math.Max(0f, durationSeconds);
            slot.RemainingCooldown.Value = Math.Max(0f, durationSeconds);
        }

        public void ReadyCooldown(string skillId)
        {
            if (!_slotsById.TryGetValue(skillId, out var slot))
            {
                return;
            }

            slot.RemainingCooldown.Value = 0f;
            slot.IsEnabled.Value = true;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            for (var i = 0; i < _slots.Count; i++)
            {
                var remaining = _slots[i].RemainingCooldown.Value;
                if (remaining <= 0f)
                {
                    continue;
                }

                _slots[i].RemainingCooldown.Value = Math.Max(0f, remaining - deltaTime);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            ClearSlots();
            CurrentHealth.Dispose();
            MaxHealth.Dispose();
            CurrentMana.Dispose();
            MaxMana.Dispose();
        }

        private void ClearSlots()
        {
            foreach (var slot in _slots)
            {
                slot.Dispose();
            }

            _slots.Clear();
            _slotsById.Clear();
        }
    }

    public sealed class SkillSlotState : IDisposable
    {
        public SkillSlotState(
            string skillId,
            SkillTargetingMode targetingMode,
            int manaCost,
            float range,
            float width)
        {
            SkillId = skillId ?? string.Empty;
            TargetingMode = targetingMode;
            Range = range;
            Width = width;
            IsEnabled = new ReactiveProperty<bool>(true);
            ManaCost = new ReactiveProperty<int>(manaCost);
            RemainingCooldown = new ReactiveProperty<float>(0f);
            CooldownDuration = new ReactiveProperty<float>(0f);
        }

        public string SkillId { get; }
        public SkillTargetingMode TargetingMode { get; }
        public float Range { get; }
        public float Width { get; }

        public ReactiveProperty<bool> IsEnabled { get; }
        public ReactiveProperty<int> ManaCost { get; }
        public ReactiveProperty<float> RemainingCooldown { get; }
        public ReactiveProperty<float> CooldownDuration { get; }

        public void Dispose()
        {
            IsEnabled.Dispose();
            ManaCost.Dispose();
            RemainingCooldown.Dispose();
            CooldownDuration.Dispose();
        }
    }
}
