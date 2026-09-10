using System.Numerics;
using Raid.Battle.Collision;
using Raid.Battle.Entities;
using Raid.Battle.Events;
using Raid.Battle.Snapshots;
using Raid.Battle.World;

namespace Raid.Battle.Combat;

public sealed class DamageSystem(BattleWorld world)
{
    public bool ApplyDamage(
        EntityId attackerId,
        EntityId targetId,
        float amount,
        string skillId,
        bool applyOnHitEffects = true)
    {
        var target = world.Entities.Find(targetId);
        if (target is null || amount <= 0f)
        {
            return false;
        }

        var remaining = amount;
        var shieldAbsorbed = Math.Min(remaining, target.CurrentShield);
        if (shieldAbsorbed > 0f)
        {
            target.CurrentShield -= shieldAbsorbed;
            remaining -= shieldAbsorbed;
            world.Events.Add(new ShieldChangedEvent(
                world.Tick,
                EntitySnapshot.FromEntity(target),
                skillId,
                target.CurrentShield));
        }

        var applied = Math.Min(remaining, target.CurrentHealth);
        if (applied > 0f)
        {
            target.CurrentHealth -= applied;
            world.Events.Add(new DamageAppliedEvent(
                world.Tick,
                EntitySnapshot.FromEntity(target),
                attackerId,
                skillId,
                applied));
        }

        var connected = shieldAbsorbed > 0f || applied > 0f;
        if (connected)
        {
            world.Chains.RegisterHit(attackerId, skillId);
        }

        if (connected && applyOnHitEffects)
        {
            ApplyOnHitEffects(attackerId, target, skillId);
        }

        return connected;
    }

    public bool ApplyHeal(EntityId sourceId, EntityId targetId, float amount, string skillId)
    {
        var target = world.Entities.Find(targetId);
        if (target is null || amount <= 0f)
        {
            return false;
        }

        var healed = Math.Min(amount, target.MaxHealth - target.CurrentHealth);
        if (healed <= 0f)
        {
            return false;
        }

        target.CurrentHealth += healed;
        world.Events.Add(new HealAppliedEvent(
            world.Tick,
            EntitySnapshot.FromEntity(target),
            sourceId,
            skillId,
            healed));
        return true;
    }

    public void ApplyShield(BattleEntity target, float amount, string skillId)
    {
        if (amount <= 0f)
        {
            return;
        }

        target.CurrentShield += amount;
        world.Events.Add(new ShieldChangedEvent(
            world.Tick,
            EntitySnapshot.FromEntity(target),
            skillId,
            target.CurrentShield));
    }

    public void ReduceShield(BattleEntity target, float amount, string skillId)
    {
        if (amount <= 0f || target.CurrentShield <= 0f)
        {
            return;
        }

        target.CurrentShield = Math.Max(0f, target.CurrentShield - amount);
        world.Events.Add(new ShieldChangedEvent(
            world.Tick,
            EntitySnapshot.FromEntity(target),
            skillId,
            target.CurrentShield));
    }

    public void EnqueueAreaHits(
        EntityId attackerId,
        Vector2 origin,
        float radius,
        string skillId,
        float amount)
    {
        if (amount <= 0f || radius <= 0f)
        {
            return;
        }

        foreach (var entity in world.Entities.All())
        {
            if (entity.Id == attackerId)
            {
                continue;
            }

            var distance = Vector2.Distance(origin, entity.Position) - entity.CollisionRadius;
            if (distance > radius)
            {
                continue;
            }

            world.Hits.Enqueue(new HitRequest(attackerId, entity.Id, skillId, amount));
        }
    }

    public void EnqueueLineHits(
        EntityId attackerId,
        Vector2 origin,
        Vector2 direction,
        float length,
        float width,
        string skillId,
        float amount)
    {
        if (amount <= 0f || length <= 0f || direction == Vector2.Zero)
        {
            return;
        }

        var normalized = Vector2.Normalize(direction);
        var end = origin + (normalized * length);
        var halfWidth = Math.Max(0f, width * 0.5f);

        foreach (var entity in world.Entities.All())
        {
            if (entity.Id == attackerId)
            {
                continue;
            }

            if (!CollisionHelper.CircleIntersectsCapsule(
                    entity.Position,
                    entity.CollisionRadius,
                    origin,
                    end,
                    halfWidth))
            {
                continue;
            }

            world.Hits.Enqueue(new HitRequest(attackerId, entity.Id, skillId, amount));
        }
    }

    private void ApplyOnHitEffects(EntityId attackerId, BattleEntity target, string skillId)
    {
        if (world.Entities.Find(attackerId) is not PlayerEntity attacker)
        {
            return;
        }

        var skill = attacker.FindSkill(skillId);
        if (skill is null)
        {
            return;
        }

        SkillEffectApplier.Apply(
            world,
            SkillEffectTrigger.OnHit,
            skill,
            attacker,
            target);
    }
}
