using Raid.Battle.Actions;
using Raid.Battle.Snapshots;

namespace Raid.Battle.Events;

public sealed record ActionPhaseChangedEvent(
    long Tick,
    EntitySnapshot Entity,
    ActionId ActionId,
    string SkillId,
    ActionPhaseKind Phase) : IBattleEvent;
