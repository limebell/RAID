using Raid.Battle.Effects;
using Raid.Battle.Entities;
using Raid.Battle.World;
using Raid.Contracts.Common;
using System.Numerics;

namespace Raid.Battle.Combat;

public static class SkillEffectApplier
{
    public static void Apply(
        BattleWorld world,
        SkillEffectTrigger trigger,
        SkillDefinition skill,
        BattleEntity source,
        BattleEntity? target,
        Vector2? point = null)
    {
        var effectTarget = target ?? source;
        foreach (var spec in skill.Effects ?? [])
        {
            if (spec.Trigger != trigger)
            {
                continue;
            }

            ApplySpec(world, skill, spec, source, effectTarget, point);
        }
    }

    public static void RemoveWhileHolding(BattleWorld world, BattleEntity owner, SkillDefinition skill)
    {
        foreach (var spec in skill.Effects ?? [])
        {
            if (spec.Trigger != SkillEffectTrigger.WhileHolding)
            {
                continue;
            }

            if (spec.Shield > 0f)
            {
                world.Combat.ReduceShield(owner, spec.Shield, skill.SkillId);
            }

            if (spec.Status is StatusEffectKind kind)
            {
                world.Effects.Remove(owner.Id, kind, skill.SkillId);
            }

            if (!string.IsNullOrWhiteSpace(spec.BuffId))
            {
                world.Effects.RemoveBuff(owner.Id, spec.BuffId);
            }
        }
    }

    private static void ApplySpec(
        BattleWorld world,
        SkillDefinition skill,
        SkillEffectSpec spec,
        BattleEntity source,
        BattleEntity target,
        Vector2? point)
    {
        if (spec.Heal > 0f)
        {
            world.Combat.ApplyHeal(source.Id, target.Id, spec.Heal, skill.SkillId);
        }

        if (spec.Shield > 0f)
        {
            world.Combat.ApplyShield(target, spec.Shield, skill.SkillId);
        }

        if (spec.ManaRestorePercentOfMax > 0f && source is PlayerEntity player)
        {
            world.Resources.RestoreMana(
                player,
                player.MaxMana * (spec.ManaRestorePercentOfMax / 100f));
        }

        if (spec.ZoneRadius > 0f && spec.ZoneDurationMilliseconds > 0)
        {
            world.Zones.Spawn(new Zone(
                source.Id,
                skill.SkillId,
                point ?? target.Position,
                spec.ZoneRadius,
                spec.ZoneDurationMilliseconds / 1000f,
                spec.ZoneTickIntervalMilliseconds / 1000f,
                spec.ZoneTickDamage,
                spec.ZoneTickHeal,
                spec.ZoneManaRestoreOnEnemyDamage));
        }

        if (!string.IsNullOrWhiteSpace(spec.BuffId))
        {
            var buffDuration = spec.DurationMilliseconds > 0
                ? spec.DurationMilliseconds / 1000f
                : float.PositiveInfinity;
            world.Effects.Apply(new StatusEffect(
                target.Id,
                StatusEffectKind.Buff,
                source.Id,
                skill.SkillId,
                buffDuration,
                buffId: spec.BuffId));
            return;
        }

        if (spec.Status is not StatusEffectKind kind)
        {
            return;
        }

        var duration = spec.DurationMilliseconds > 0
            ? spec.DurationMilliseconds / 1000f
            : float.PositiveInfinity;
        world.Effects.Apply(new StatusEffect(
            target.Id,
            kind,
            source.Id,
            skill.SkillId,
            duration,
            spec.TickIntervalMilliseconds / 1000f,
            spec.TickDamage));
    }
}
