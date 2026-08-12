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
        string skillId)
    {
        var target = world.Entities.Find(targetId);
        if (target is null || amount <= 0f)
        {
            return false;
        }

        var applied = Math.Min(amount, target.CurrentHealth);
        target.CurrentHealth -= applied;

        world.Events.Add(new DamageAppliedEvent(
            world.Tick,
            EntitySnapshot.FromEntity(target),
            attackerId,
            skillId,
            applied));

        return true;
    }
}
