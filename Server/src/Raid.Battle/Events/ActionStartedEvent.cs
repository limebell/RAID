using Raid.Battle.Actions;
using Raid.Battle.Entities;

namespace Raid.Battle.Events;

public sealed record ActionStartedEvent(
    long Tick,
    EntityId OwnerId,
    ActionId ActionId,
    string SkillId,
    ActionPhaseKind? Phase) : IBattleEvent;
