using Raid.Battle.Actions;
using Raid.Battle.Commands;
using Raid.Battle.Entities;
using Raid.Battle.Snapshots;

namespace Raid.Battle.Events;

public sealed record ActionPhaseChangedEvent(
    long Tick,
    EntitySnapshot Entity,
    ActionId ActionId,
    string SkillId,
    ActionPhaseKind Phase,
    float DurationSeconds = 0f,
    SkillTarget? Target = null) : IBattleEvent;
