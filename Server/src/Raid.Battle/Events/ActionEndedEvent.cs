using Raid.Battle.Actions;
using Raid.Battle.Snapshots;

namespace Raid.Battle.Events;

public sealed record ActionEndedEvent(
    long Tick,
    EntitySnapshot Entity,
    ActionId ActionId,
    string SkillId,
    ActionEndReason Reason) : IBattleEvent;
