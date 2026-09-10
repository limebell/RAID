using System.Numerics;
using Raid.Battle.Entities;
using Raid.Battle.World;

namespace Raid.Battle.Combat;

public sealed class DelayedHitSystem(BattleWorld world)
{
    private readonly List<DelayedEntityHit> _entityHits = [];
    private readonly List<DelayedAreaHit> _areaHits = [];

    public void Schedule(HitRequest request, int delayTicks)
    {
        if (delayTicks <= 0)
        {
            world.Hits.Enqueue(request);
            return;
        }

        _entityHits.Add(new DelayedEntityHit(world.Tick + delayTicks, request));
    }

    public void ScheduleArea(
        EntityId attackerId,
        Vector2 origin,
        float radius,
        string skillId,
        float amount,
        int delayTicks)
    {
        if (delayTicks <= 0)
        {
            world.Combat.EnqueueAreaHits(attackerId, origin, radius, skillId, amount);
            return;
        }

        _areaHits.Add(new DelayedAreaHit(
            world.Tick + delayTicks,
            attackerId,
            origin,
            radius,
            skillId,
            amount));
    }

    public void Update()
    {
        for (var i = _entityHits.Count - 1; i >= 0; i--)
        {
            if (world.Tick < _entityHits[i].DueTick)
            {
                continue;
            }

            world.Hits.Enqueue(_entityHits[i].Request);
            _entityHits.RemoveAt(i);
        }

        for (var i = _areaHits.Count - 1; i >= 0; i--)
        {
            var hit = _areaHits[i];
            if (world.Tick < hit.DueTick)
            {
                continue;
            }

            world.Combat.EnqueueAreaHits(
                hit.AttackerId,
                hit.Origin,
                hit.Radius,
                hit.SkillId,
                hit.Amount);
            _areaHits.RemoveAt(i);
        }
    }

    private readonly record struct DelayedEntityHit(long DueTick, HitRequest Request);

    private readonly record struct DelayedAreaHit(
        long DueTick,
        EntityId AttackerId,
        Vector2 Origin,
        float Radius,
        string SkillId,
        float Amount);
}
