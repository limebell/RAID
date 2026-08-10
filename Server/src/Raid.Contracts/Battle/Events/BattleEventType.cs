namespace Raid.Contracts.Battle.Events;

public enum BattleEventType
{
    Unknown = 0,
    EntityMoved = 1,
    EntityMoveCompleted = 2,
    PositionSet = 3,
    ActionStarted = 4,
    ActionPhaseChanged = 5,
    ActionEnded = 6,
    DamageApplied = 7
}
