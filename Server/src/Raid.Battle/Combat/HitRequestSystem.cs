using System.Collections.Concurrent;
using Raid.Battle.World;

namespace Raid.Battle.Combat;

public sealed class HitRequestSystem(BattleWorld world)
{
    private readonly ConcurrentQueue<HitRequest> _requests = [];

    public void Enqueue(HitRequest request)
    {
        _requests.Enqueue(request);
    }

    public void ProcessPending()
    {
        while (_requests.TryDequeue(out var request))
        {
            world.Combat.ApplyDamage(
                request.AttackerId,
                request.TargetId,
                request.Damage,
                request.SkillId);
        }
    }
}
