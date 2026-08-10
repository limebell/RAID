using Raid.Battle.Entities;
using Raid.Battle.Events;
using Raid.Contracts.Battle.Events;
using Raid.Contracts.Battle.Snapshots;
using Raid.Contracts.Common;

namespace Raid.GameServer.Sessions;

public static class BattleDtoMapper
{
    public static BattleSnapshotDto ToSnapshot(RaidSession session)
    {
        var entities = session.World.Entities.All()
            .Select(ToEntitySnapshot)
            .ToArray();

        return new BattleSnapshotDto(
            session.Id,
            session.Mode,
            session.World.Tick,
            entities);
    }

    public static EntitySnapshotDto ToEntitySnapshot(BattleEntity entity)
    {
        return new EntitySnapshotDto(
            entity.Id.Value,
            entity.Kind.ToString(),
            new Vector2Dto(entity.Position.X, entity.Position.Y),
            entity.CurrentHealth,
            entity.MaxHealth,
            entity.Actions.IsBusy,
            entity.Actions.CurrentAction?.CurrentPhaseKind?.ToString());
    }

    public static BattleEventDto ToEventDto(IBattleEvent battleEvent)
    {
        return battleEvent switch
        {
            EntityMovedEvent moved => new BattleEventDto(
                Type: BattleEventType.EntityMoved,
                Tick: moved.Tick,
                EntityId: moved.EntityId.Value,
                Position: new Vector2Dto(moved.Position.X, moved.Position.Y)),
            EntityMoveCompletedEvent completed => new BattleEventDto(
                Type: BattleEventType.EntityMoveCompleted,
                Tick: completed.Tick,
                EntityId: completed.EntityId.Value,
                Position: new Vector2Dto(completed.Position.X, completed.Position.Y)),
            PositionSetEvent positionSet => new BattleEventDto(
                Type: BattleEventType.PositionSet,
                Tick: positionSet.Tick,
                EntityId: positionSet.EntityId.Value,
                Position: new Vector2Dto(positionSet.Position.X, positionSet.Position.Y),
                Reason: positionSet.Reason.ToString()),
            ActionStartedEvent started => new BattleEventDto(
                Type: BattleEventType.ActionStarted,
                Tick: started.Tick,
                OwnerId: started.OwnerId.Value,
                SkillId: started.SkillId,
                Phase: started.Phase?.ToString()),
            ActionPhaseChangedEvent phaseChanged => new BattleEventDto(
                Type: BattleEventType.ActionPhaseChanged,
                Tick: phaseChanged.Tick,
                OwnerId: phaseChanged.OwnerId.Value,
                SkillId: phaseChanged.SkillId,
                Phase: phaseChanged.Phase.ToString()),
            ActionEndedEvent ended => new BattleEventDto(
                Type: BattleEventType.ActionEnded,
                Tick: ended.Tick,
                OwnerId: ended.OwnerId.Value,
                SkillId: ended.SkillId,
                Reason: ended.Reason.ToString()),
            DamageAppliedEvent damage => new BattleEventDto(
                Type: BattleEventType.DamageApplied,
                Tick: damage.Tick,
                AttackerId: damage.AttackerId.Value,
                TargetEntityId: damage.TargetId.Value,
                SkillId: damage.SkillId,
                Amount: damage.Amount,
                RemainingHealth: damage.TargetRemainingHealth),
            _ => new BattleEventDto(
                Type: BattleEventType.Unknown,
                Tick: battleEvent.Tick)
        };
    }
}
