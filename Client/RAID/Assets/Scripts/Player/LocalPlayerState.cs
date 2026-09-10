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
        private readonly HashSet<string> _activeBuffIds = new(StringComparer.Ordinal);
        private bool _disposed;
        private float _gaugeDurationSeconds;
        private float _gaugeElapsedSeconds;

        public ReactiveProperty<float> CurrentHealth { get; } = new(0f);
        public ReactiveProperty<float> MaxHealth { get; } = new(0f);
        public ReactiveProperty<float> CurrentMana { get; } = new(0f);
        public ReactiveProperty<float> MaxMana { get; } = new(0f);
        public ReactiveProperty<bool> ActionGaugeVisible { get; } = new(false);
        public ReactiveProperty<string> ActionGaugePhase { get; } = new(string.Empty);
        public ReactiveProperty<float> ActionGaugeProgress { get; } = new(0f);
        public ReactiveProperty<bool> TargetHealthVisible { get; } = new(false);
        public ReactiveProperty<string> TargetName { get; } = new(string.Empty);
        public ReactiveProperty<float> TargetCurrentHealth { get; } = new(0f);
        public ReactiveProperty<float> TargetMaxHealth { get; } = new(0f);

        public long? LastAttackedEntityId { get; private set; }

        public IReadOnlyList<SkillSlotState> Slots => _slots;

        public SkillSlotState BasicAttack
        {
            get
            {
                SkillSlotState fallback = null;
                foreach (var slot in _slotsById.Values)
                {
                    if (!slot.IsBasicAttack)
                    {
                        continue;
                    }

                    fallback ??= slot;
                    if (IsAvailable(slot))
                    {
                        return slot;
                    }
                }

                if (fallback is null)
                {
                    throw new InvalidOperationException("No basic attack skill is loaded.");
                }

                return fallback;
            }
        }

        public SkillSlotState GetSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), slotIndex, "Skill bar slot is out of range.");
            }

            var slot = _slots[slotIndex];
            if (slot is null)
            {
                throw new InvalidOperationException($"Skill bar slot {slotIndex} is empty.");
            }

            return Resolve(slot);
        }

        public bool TryGetSlotBySkillId(string skillId, out SkillSlotState slot)
        {
            return _slotsById.TryGetValue(skillId, out slot);
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

        public void RememberAttackedTarget(EntitySnapshotDto entity)
        {
            LastAttackedEntityId = entity.EntityId;
            TargetName.Value = FormatTargetName(entity);
            TargetCurrentHealth.Value = entity.CurrentHealth;
            TargetMaxHealth.Value = entity.MaxHealth;
            TargetHealthVisible.Value = true;
        }

        public void ApplyTargetVitalsIfTracked(EntitySnapshotDto entity)
        {
            if (LastAttackedEntityId != entity.EntityId)
            {
                return;
            }

            TargetCurrentHealth.Value = entity.CurrentHealth;
            TargetMaxHealth.Value = entity.MaxHealth;
        }

        public void SetSkills(IReadOnlyList<SkillInfoDto> skills)
        {
            ClearSlots();

            var bar = new SkillSlotState[PlayerOptions.SkillSlotCount];
            var useBarSlots = false;
            foreach (var skill in skills)
            {
                if (skill.BarSlot != null)
                {
                    useBarSlots = true;
                    break;
                }
            }

            var fallbackIndex = 0;
            foreach (var skill in skills)
            {
                if (_slotsById.ContainsKey(skill.SkillId))
                {
                    throw new InvalidOperationException($"Duplicate skill '{skill.SkillId}'.");
                }

                var slot = new SkillSlotState(
                    skill.SkillId,
                    skill.Kind,
                    skill.TargetingMode,
                    skill.ManaCost,
                    skill.Range,
                    skill.Width,
                    skill.CastTimeMilliseconds,
                    skill.ChannelTimeMilliseconds,
                    skill.ProjectileSpeed,
                    skill.IsBasicAttack,
                    skill.RequiredBuffId,
                    skill.VariantGroup);
                _slotsById[skill.SkillId] = slot;

                if (!string.IsNullOrEmpty(skill.ToggleOffBuffId) &&
                    !string.IsNullOrEmpty(skill.ToggleOnBuffId))
                {
                    _activeBuffIds.Add(skill.ToggleOffBuffId);
                }

                if (skill.IsBasicAttack)
                {
                    continue;
                }

                if (useBarSlots)
                {
                    if (skill.BarSlot is not SkillBarSlot barSlot)
                    {
                        continue;
                    }

                    var index = (int)barSlot;
                    if (index < 0 || index >= bar.Length)
                    {
                        throw new InvalidOperationException(
                            $"Skill '{skill.SkillId}' bar slot {barSlot} is out of range.");
                    }

                    if (bar[index] is not null)
                    {
                        throw new InvalidOperationException(
                            $"Skill bar slot {barSlot} is already occupied by '{bar[index].SkillId}'.");
                    }

                    bar[index] = slot;
                    continue;
                }

                if (fallbackIndex >= bar.Length)
                {
                    throw new InvalidOperationException(
                        $"Skill '{skill.SkillId}' exceeds the skill bar size.");
                }

                bar[fallbackIndex++] = slot;
            }

            _slots.AddRange(bar);
        }

        public void ApplyBuff(string buffId, bool applied)
        {
            if (applied)
            {
                _activeBuffIds.Add(buffId);
                return;
            }

            _activeBuffIds.Remove(buffId);
        }

        public SkillSlotState Resolve(SkillSlotState slot)
        {
            if (string.IsNullOrEmpty(slot.VariantGroup))
            {
                return slot;
            }

            SkillSlotState fallback = slot;
            foreach (var candidate in _slotsById.Values)
            {
                if (!string.Equals(candidate.VariantGroup, slot.VariantGroup, StringComparison.Ordinal))
                {
                    continue;
                }

                if (IsAvailable(candidate))
                {
                    return candidate;
                }
            }

            return fallback;
        }

        public void DisableSkill(string skillId)
        {
            ForSkillAndVariants(skillId, slot => slot.IsEnabled.Value = false);
        }

        public void StartCooldown(string skillId, float durationSeconds)
        {
            var duration = Math.Max(0f, durationSeconds);
            ForSkillAndVariants(skillId, slot =>
            {
                slot.CooldownDuration.Value = duration;
                slot.RemainingCooldown.Value = duration;
            });
        }

        public void ReadyCooldown(string skillId)
        {
            ForSkillAndVariants(skillId, slot =>
            {
                slot.RemainingCooldown.Value = 0f;
                slot.IsEnabled.Value = true;
            });
        }

        public void StartActionGauge(string phase, string skillId, float? durationSeconds)
        {
            if (!IsGaugePhase(phase))
            {
                StopActionGauge();
                return;
            }

            var duration = durationSeconds ?? 0f;
            if (duration <= 0f)
            {
                duration = ResolveGaugeDuration(phase, skillId);
            }

            if (duration <= 0f)
            {
                StopActionGauge();
                return;
            }

            _gaugeDurationSeconds = duration;
            _gaugeElapsedSeconds = 0f;
            ActionGaugePhase.Value = phase;
            ActionGaugeProgress.Value = 0f;
            ActionGaugeVisible.Value = true;
        }

        public void StopActionGauge()
        {
            _gaugeDurationSeconds = 0f;
            _gaugeElapsedSeconds = 0f;
            ActionGaugeVisible.Value = false;
            ActionGaugeProgress.Value = 0f;
            ActionGaugePhase.Value = string.Empty;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            TickActionGauge(deltaTime);

            foreach (var slot in _slotsById.Values)
            {
                var remaining = slot.RemainingCooldown.Value;
                if (remaining <= 0f)
                {
                    continue;
                }

                slot.RemainingCooldown.Value = Math.Max(0f, remaining - deltaTime);
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
            ActionGaugeVisible.Dispose();
            ActionGaugePhase.Dispose();
            ActionGaugeProgress.Dispose();
            TargetHealthVisible.Dispose();
            TargetName.Dispose();
            TargetCurrentHealth.Dispose();
            TargetMaxHealth.Dispose();
        }

        private static string FormatTargetName(EntitySnapshotDto entity)
        {
            var id = entity.DefinitionId;
            if (string.IsNullOrEmpty(id))
            {
                return entity.Kind.ToString();
            }

            var separator = id.LastIndexOf('.');
            var name = separator >= 0 && separator < id.Length - 1
                ? id[(separator + 1)..]
                : id;
            if (name.Length == 0)
            {
                return entity.Kind.ToString();
            }

            return char.ToUpperInvariant(name[0]) + name[1..];
        }

        private void TickActionGauge(float deltaTime)
        {
            if (!ActionGaugeVisible.Value || _gaugeDurationSeconds <= 0f)
            {
                return;
            }

            _gaugeElapsedSeconds = Math.Min(
                _gaugeDurationSeconds,
                _gaugeElapsedSeconds + deltaTime);
            ActionGaugeProgress.Value = _gaugeElapsedSeconds / _gaugeDurationSeconds;
        }

        private float ResolveGaugeDuration(string phase, string skillId)
        {
            if (!_slotsById.TryGetValue(skillId, out var slot))
            {
                throw new InvalidOperationException($"Unknown skill '{skillId}'.");
            }

            if (string.Equals(phase, "Casting", StringComparison.OrdinalIgnoreCase))
            {
                return slot.CastTimeMilliseconds / 1000f;
            }

            if (string.Equals(phase, "Holding", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(phase, "Charging", StringComparison.OrdinalIgnoreCase))
            {
                return slot.ChannelTimeMilliseconds / 1000f;
            }

            return 0f;
        }

        private static bool IsGaugePhase(string phase)
        {
            return string.Equals(phase, "Casting", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(phase, "Holding", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(phase, "Charging", StringComparison.OrdinalIgnoreCase);
        }

        private void ClearSlots()
        {
            foreach (var slot in _slotsById.Values)
            {
                slot.Dispose();
            }

            _slots.Clear();
            _slotsById.Clear();
            _activeBuffIds.Clear();
        }

        private bool IsAvailable(SkillSlotState slot)
        {
            return string.IsNullOrEmpty(slot.RequiredBuffId)
                || _activeBuffIds.Contains(slot.RequiredBuffId);
        }

        private void ForSkillAndVariants(string skillId, Action<SkillSlotState> apply)
        {
            if (!_slotsById.TryGetValue(skillId, out var slot))
            {
                throw new InvalidOperationException($"Unknown skill '{skillId}'.");
            }

            apply(slot);
            if (string.IsNullOrEmpty(slot.VariantGroup))
            {
                return;
            }

            foreach (var candidate in _slotsById.Values)
            {
                if (candidate == slot ||
                    !string.Equals(candidate.VariantGroup, slot.VariantGroup, StringComparison.Ordinal))
                {
                    continue;
                }

                apply(candidate);
            }
        }
    }
}
