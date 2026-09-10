using System.Numerics;
using Raid.Battle.Entities;

namespace Raid.Battle.Events;

public sealed class BattleEventBuffer
{
    private readonly List<IBattleEvent> _events = [];

    public void Add(IBattleEvent battleEvent)
    {
        _events.Add(battleEvent);
    }

    public IReadOnlyList<IBattleEvent> Drain()
    {
        if (_events.Count == 0)
        {
            return [];
        }

        var drained = _events.ToArray();
        _events.Clear();
        return drained;
    }
}
