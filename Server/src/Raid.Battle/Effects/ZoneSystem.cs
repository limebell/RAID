using System.Numerics;
using Raid.Battle.Combat;
using Raid.Battle.Entities;
using Raid.Battle.Events;
using Raid.Battle.Snapshots;
using Raid.Battle.World;

namespace Raid.Battle.Effects;

public sealed class ZoneSystem(BattleWorld world)
{
    private readonly List<Zone> _zones = [];

    public void Spawn(Zone zone)
    {
        _zones.Add(zone);
        var owner = world.Entities.Find(zone.OwnerId);
        if (owner is not null)
        {
            world.Events.Add(new ZoneSpawnedEvent(
                world.Tick,
                EntitySnapshot.FromEntity(owner),
                zone.SkillId,
                zone.Position,
                zone.Radius,
                zone.RemainingSeconds));
        }

        ApplyTick(zone);
    }

    public void Update(float deltaTime)
    {
        for (var i = _zones.Count - 1; i >= 0; i--)
        {
            var zone = _zones[i];
            Tick(zone, deltaTime);
            zone.RemainingSeconds -= deltaTime;
            if (zone.RemainingSeconds > 0f)
            {
                continue;
            }

            _zones.RemoveAt(i);
            var owner = world.Entities.Find(zone.OwnerId);
            if (owner is null)
            {
                continue;
            }

            world.Events.Add(new ZoneExpiredEvent(
                world.Tick,
                EntitySnapshot.FromEntity(owner),
                zone.SkillId,
                zone.Position));
        }
    }

    private void Tick(Zone zone, float deltaTime)
    {
        if (zone.TickIntervalSeconds <= 0f)
        {
            return;
        }

        zone.TickElapsedSeconds += deltaTime;
        while (zone.TickElapsedSeconds >= zone.TickIntervalSeconds)
        {
            zone.TickElapsedSeconds -= zone.TickIntervalSeconds;
            ApplyTick(zone);
        }
    }

    private void ApplyTick(Zone zone)
    {
        var owner = world.Entities.Find(zone.OwnerId);
        if (owner is null)
        {
            return;
        }

        foreach (var entity in world.Entities.All())
        {
            var distance = Vector2.Distance(zone.Position, entity.Position) - entity.CollisionRadius;
            if (distance > zone.Radius)
            {
                continue;
            }

            if (zone.TickDamage > 0f && SkillCaster.IsLivingHostile(owner, entity))
            {
                var damaged = world.Combat.ApplyDamage(
                    owner.Id,
                    entity.Id,
                    zone.TickDamage,
                    zone.SkillId,
                    applyOnHitEffects: false);
                if (damaged
                    && zone.ManaRestoreOnEnemyDamage > 0f
                    && owner is PlayerEntity player)
                {
                    world.Resources.RestoreMana(player, zone.ManaRestoreOnEnemyDamage);
                }
            }

            if (zone.TickHeal > 0f && SkillCaster.IsLivingAlly(entity))
            {
                world.Combat.ApplyHeal(owner.Id, entity.Id, zone.TickHeal, zone.SkillId);
            }
        }
    }
}
