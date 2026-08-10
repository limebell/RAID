using Raid.Battle.Actions;
using Raid.Battle.Entities;

namespace Raid.Battle.Events;

public sealed record ActionPhaseChangedEvent(
    long Tick,
    EntityId OwnerId,
    ActionId ActionId,
    string SkillId,
    ActionPhaseKind Phase) : IBattleEvent;
