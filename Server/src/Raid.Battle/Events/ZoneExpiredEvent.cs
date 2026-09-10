using System.Numerics;
using Raid.Battle.Snapshots;

namespace Raid.Battle.Events;

public sealed record ZoneExpiredEvent(
    long Tick,
    EntitySnapshot Owner,
    string SkillId,
    Vector2 Position) : IBattleEvent;
