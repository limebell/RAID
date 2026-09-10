using System.Numerics;
using Raid.Battle.Snapshots;

namespace Raid.Battle.Events;

public sealed record ZoneSpawnedEvent(
    long Tick,
    EntitySnapshot Owner,
    string SkillId,
    Vector2 Position,
    float Radius,
    float DurationSeconds) : IBattleEvent;
