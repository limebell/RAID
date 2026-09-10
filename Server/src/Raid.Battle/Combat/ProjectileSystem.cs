using System.Numerics;
using Raid.Battle.Collision;
using Raid.Battle.Entities;
using Raid.Battle.World;

namespace Raid.Battle.Combat;

public sealed class ProjectileSystem(BattleWorld world)
{
    private readonly List<DirectionProjectile> _directionProjectiles = [];

    public void SpawnDirection(
        EntityId attackerId,
        Vector2 origin,
        Vector2 direction,
        float range,
        float radius,
        float speed,
        string skillId,
        float damage)
    {
        if (range <= 0f || speed <= 0f || direction == Vector2.Zero || damage <= 0f)
        {
            return;
        }

        _directionProjectiles.Add(new DirectionProjectile(
            attackerId,
            origin,
            Vector2.Normalize(direction),
            range,
            Math.Max(0.05f, radius),
            speed,
            skillId,
            damage));
    }

    public void Update(float deltaTime)
    {
        for (var i = _directionProjectiles.Count - 1; i >= 0; i--)
        {
            if (Advance(_directionProjectiles[i], deltaTime))
            {
                continue;
            }

            _directionProjectiles.RemoveAt(i);
        }
    }

    private bool Advance(DirectionProjectile projectile, float deltaTime)
    {
        var step = Math.Min(projectile.Speed * deltaTime, projectile.RemainingDistance);
        if (step <= 0f)
        {
            return false;
        }

        var next = projectile.Position + (projectile.Direction * step);
        if (TryHit(projectile, projectile.Position, next))
        {
            return false;
        }

        projectile.Position = next;
        projectile.RemainingDistance -= step;
        return projectile.RemainingDistance > 0f;
    }

    private bool TryHit(DirectionProjectile projectile, Vector2 from, Vector2 to)
    {
        BattleEntity? closest = null;
        var closestAlong = float.MaxValue;

        foreach (var entity in world.Entities.All())
        {
            if (entity.Id == projectile.AttackerId)
            {
                continue;
            }

            if (!CollisionHelper.CircleIntersectsCapsule(
                    entity.Position,
                    entity.CollisionRadius,
                    from,
                    to,
                    projectile.Radius))
            {
                continue;
            }

            var point = CollisionHelper.ClosestPointOnSegment(entity.Position, from, to);
            var along = Vector2.Dot(point - from, projectile.Direction);
            if (along >= closestAlong)
            {
                continue;
            }

            closestAlong = along;
            closest = entity;
        }

        if (closest is null)
        {
            return false;
        }

        world.Hits.Enqueue(new HitRequest(
            projectile.AttackerId,
            closest.Id,
            projectile.SkillId,
            projectile.Damage));
        return true;
    }

    private sealed class DirectionProjectile(
        EntityId attackerId,
        Vector2 origin,
        Vector2 direction,
        float range,
        float radius,
        float speed,
        string skillId,
        float damage)
    {
        public EntityId AttackerId { get; } = attackerId;

        public Vector2 Direction { get; } = direction;

        public float Radius { get; } = radius;

        public float Speed { get; } = speed;

        public string SkillId { get; } = skillId;

        public float Damage { get; } = damage;

        public Vector2 Position { get; set; } = origin;

        public float RemainingDistance { get; set; } = range;
    }
}
