using Raid.Battle.Actions;
using Raid.Battle.Combat;
using Raid.Battle.Entities;
using Raid.Battle.Events;
using Raid.Battle.Snapshots;
using Raid.Battle.World;
using Raid.Contracts.Common;

namespace Raid.Battle.Effects;

public sealed class StatusEffectSystem(BattleWorld world)
{
    private readonly Dictionary<EntityId, List<StatusEffect>> _effects = [];

    public bool Has(EntityId targetId, StatusEffectKind kind)
    {
        return _effects.TryGetValue(targetId, out var effects)
            && effects.Exists(effect => effect.Kind == kind);
    }

    public bool IsMovementLocked(EntityId targetId)
    {
        return Has(targetId, StatusEffectKind.Stun);
    }

    public bool IsSkillLocked(EntityId targetId)
    {
        return Has(targetId, StatusEffectKind.Stun)
            || Has(targetId, StatusEffectKind.Silence);
    }

    public bool Apply(StatusEffect effect)
    {
        var target = world.Entities.Find(effect.TargetId);
        if (target is null)
        {
            return false;
        }

        if (IsDebuff(effect.Kind) && Has(effect.TargetId, StatusEffectKind.StatusImmunity))
        {
            return false;
        }

        var effects = GetOrCreate(effect.TargetId);
        var existing = effects.Find(candidate => IsSameEffect(candidate, effect));
        if (existing is not null)
        {
            existing.RemainingSeconds = effect.RemainingSeconds;
            existing.TickElapsedSeconds = 0f;
            return true;
        }

        effects.Add(effect);
        world.Events.Add(new StatusEffectAppliedEvent(
            world.Tick,
            EntitySnapshot.FromEntity(target),
            effect.SourceId,
            effect.SkillId,
            effect.Kind,
            effect.RemainingSeconds,
            effect.BuffId));
        ApplyActionConstraint(target, effect.Kind);
        return true;
    }

    public bool HasBuff(EntityId targetId, string buffId)
    {
        return _effects.TryGetValue(targetId, out var effects)
            && effects.Exists(effect =>
                string.Equals(effect.BuffId, buffId, StringComparison.Ordinal));
    }

    public bool RemoveBuff(EntityId targetId, string buffId)
    {
        if (!_effects.TryGetValue(targetId, out var effects))
        {
            return false;
        }

        var removed = false;
        for (var i = effects.Count - 1; i >= 0; i--)
        {
            var effect = effects[i];
            if (!string.Equals(effect.BuffId, buffId, StringComparison.Ordinal))
            {
                continue;
            }

            effects.RemoveAt(i);
            EmitRemoved(effect);
            removed = true;
        }

        if (effects.Count == 0)
        {
            _effects.Remove(targetId);
        }

        return removed;
    }

    public bool RemoveFromSkill(EntityId targetId, string skillId)
    {
        if (!_effects.TryGetValue(targetId, out var effects))
        {
            return false;
        }

        var removed = false;
        for (var i = effects.Count - 1; i >= 0; i--)
        {
            var effect = effects[i];
            if (!string.Equals(effect.SkillId, skillId, StringComparison.Ordinal))
            {
                continue;
            }

            effects.RemoveAt(i);
            EmitRemoved(effect);
            removed = true;
        }

        if (effects.Count == 0)
        {
            _effects.Remove(targetId);
        }

        return removed;
    }

    public bool Remove(EntityId targetId, StatusEffectKind kind, string? skillId = null)
    {
        if (!_effects.TryGetValue(targetId, out var effects))
        {
            return false;
        }

        var removed = false;
        for (var i = effects.Count - 1; i >= 0; i--)
        {
            var effect = effects[i];
            if (effect.Kind != kind)
            {
                continue;
            }

            if (skillId is not null
                && !string.Equals(effect.SkillId, skillId, StringComparison.Ordinal))
            {
                continue;
            }

            effects.RemoveAt(i);
            EmitRemoved(effect);
            removed = true;
        }

        if (effects.Count == 0)
        {
            _effects.Remove(targetId);
        }

        return removed;
    }

    public void Update(float deltaTime)
    {
        foreach (var (targetId, effects) in _effects.ToArray())
        {
            for (var i = effects.Count - 1; i >= 0; i--)
            {
                var effect = effects[i];
                Tick(effect, deltaTime);
                if (float.IsFinite(effect.RemainingSeconds))
                {
                    effect.RemainingSeconds -= deltaTime;
                }

                if (effect.RemainingSeconds > 0f || !float.IsFinite(effect.RemainingSeconds))
                {
                    continue;
                }

                effects.RemoveAt(i);
                EmitRemoved(effect);
            }

            if (effects.Count == 0)
            {
                _effects.Remove(targetId);
            }
        }
    }

    private void Tick(StatusEffect effect, float deltaTime)
    {
        if (effect.TickDamage <= 0f || effect.TickIntervalSeconds <= 0f)
        {
            return;
        }

        effect.TickElapsedSeconds += deltaTime;
        while (effect.TickElapsedSeconds >= effect.TickIntervalSeconds)
        {
            effect.TickElapsedSeconds -= effect.TickIntervalSeconds;
            if (effect.SourceId is not EntityId sourceId)
            {
                continue;
            }

            world.Combat.ApplyDamage(
                sourceId,
                effect.TargetId,
                effect.TickDamage,
                effect.SkillId ?? string.Empty,
                applyOnHitEffects: false);
        }
    }

    private List<StatusEffect> GetOrCreate(EntityId targetId)
    {
        if (_effects.TryGetValue(targetId, out var effects))
        {
            return effects;
        }

        effects = [];
        _effects[targetId] = effects;
        return effects;
    }

    private void EmitRemoved(StatusEffect effect)
    {
        var target = world.Entities.Find(effect.TargetId);
        if (target is null)
        {
            return;
        }

        world.Events.Add(new StatusEffectRemovedEvent(
            world.Tick,
            EntitySnapshot.FromEntity(target),
            effect.SkillId,
            effect.Kind,
            effect.BuffId));
    }

    private static bool IsSameEffect(StatusEffect left, StatusEffect right)
    {
        if (left.Kind != right.Kind)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(left.BuffId) || !string.IsNullOrEmpty(right.BuffId))
        {
            return string.Equals(left.BuffId, right.BuffId, StringComparison.Ordinal);
        }

        return string.Equals(left.SkillId, right.SkillId, StringComparison.Ordinal);
    }

    private static bool IsDebuff(StatusEffectKind kind)
    {
        return kind is StatusEffectKind.Bleed or StatusEffectKind.Stun or StatusEffectKind.Silence;
    }

    private void ApplyActionConstraint(BattleEntity target, StatusEffectKind kind)
    {
        if (target is not PlayerEntity player)
        {
            return;
        }

        if (kind == StatusEffectKind.Stun)
        {
            player.CombatOrder.Clear();
            world.Movement.ClearIntent(player.Id);
            world.Actions.Cancel(player, ActionEndReason.CancelledByInterrupt);
            return;
        }

        if (kind == StatusEffectKind.Silence && player.Actions.IsCasting)
        {
            world.Actions.Cancel(player, ActionEndReason.CancelledByInterrupt);
        }
    }
}
