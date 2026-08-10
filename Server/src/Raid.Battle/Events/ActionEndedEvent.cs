using Raid.Battle.Actions;
using Raid.Battle.Entities;

namespace Raid.Battle.Events;

public sealed record ActionEndedEvent(
    long Tick,
    EntityId OwnerId,
    ActionId ActionId,
    string SkillId,
    ActionEndReason Reason) : IBattleEvent;
