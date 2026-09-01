using Raid.Battle.Entities;
using Raid.Battle.Events;
using Raid.Battle.Snapshots;
using Raid.Battle.World;
using Raid.Contracts.Battle.Events;
using Raid.Contracts.Battle.Snapshots;
using Raid.Contracts.Session;
using Raid.Contracts.Common;

namespace Raid.GameServer.Sessions;

public static class BattleDtoMapper
{
    public static BattleSnapshotDto ToSnapshot(RaidSession session)
    {
        var entities = session.World.Entities.All()
            .Select(EntitySnapshot.FromEntity)
            .Select(ToEntitySnapshotDto)
            .ToArray();

        return new BattleSnapshotDto(
            session.Id,
            session.Mode,
            session.World.Tick,
            entities);
    }

    public static EntitySnapshotDto ToEntitySnapshotDto(BattleEntity entity)
    {
        return ToEntitySnapshotDto(EntitySnapshot.FromEntity(entity));
    }

    public static PracticeSettingsDto ToPracticeSettingsDto(PracticeSettings settings)
    {
        return new PracticeSettingsDto(
            settings.HighManaRegen,
            settings.IgnoreCooldowns);
    }

    public static EntitySnapshotDto ToEntitySnapshotDto(EntitySnapshot snapshot)
    {
        return new EntitySnapshotDto(
            snapshot.EntityId.Value,
            snapshot.Kind,
            new Vector2Dto(snapshot.Position.X, snapshot.Position.Y),
            new Vector2Dto(snapshot.FacingDirection.X, snapshot.FacingDirection.Y),
            snapshot.CurrentHealth,
            snapshot.MaxHealth,
            snapshot.CurrentMana,
            snapshot.MaxMana,
            snapshot.IsBusy,
            snapshot.CurrentPhase);
    }

    public static BattleEventDto ToEventDto(IBattleEvent battleEvent)
    {
        return battleEvent switch
        {
            EntityMovedEvent moved => new BattleEventDto(
                Type: BattleEventType.EntityMoved,
                Tick: moved.Tick,
                Entity: ToEntitySnapshotDto(moved.Entity)),
            EntityMoveCompletedEvent completed => new BattleEventDto(
                Type: BattleEventType.EntityMoveCompleted,
                Tick: completed.Tick,
                Entity: ToEntitySnapshotDto(completed.Entity)),
            PositionSetEvent positionSet => new BattleEventDto(
                Type: BattleEventType.PositionSet,
                Tick: positionSet.Tick,
                Entity: ToEntitySnapshotDto(positionSet.Entity),
                Reason: positionSet.Reason.ToString()),
            ActionStartedEvent started => new BattleEventDto(
                Type: BattleEventType.ActionStarted,
                Tick: started.Tick,
                Entity: ToEntitySnapshotDto(started.Entity),
                SkillId: started.SkillId,
                Phase: started.Phase?.ToString()),
            ActionPhaseChangedEvent phaseChanged => new BattleEventDto(
                Type: BattleEventType.ActionPhaseChanged,
                Tick: phaseChanged.Tick,
                Entity: ToEntitySnapshotDto(phaseChanged.Entity),
                SkillId: phaseChanged.SkillId,
                Phase: phaseChanged.Phase.ToString()),
            ActionEndedEvent ended => new BattleEventDto(
                Type: BattleEventType.ActionEnded,
                Tick: ended.Tick,
                Entity: ToEntitySnapshotDto(ended.Entity),
                SkillId: ended.SkillId,
                Reason: ended.Reason.ToString()),
            DamageAppliedEvent damage => new BattleEventDto(
                Type: BattleEventType.DamageApplied,
                Tick: damage.Tick,
                Entity: ToEntitySnapshotDto(damage.Target),
                AttackerId: damage.AttackerId.Value,
                SkillId: damage.SkillId,
                Amount: damage.Amount),
            EntitySpawnedEvent spawned => new BattleEventDto(
                Type: BattleEventType.EntitySpawned,
                Tick: spawned.Tick,
                Entity: ToEntitySnapshotDto(spawned.Entity)),
            ResourceChangedEvent resourceChanged => new BattleEventDto(
                Type: BattleEventType.ResourceChanged,
                Tick: resourceChanged.Tick,
                Entity: ToEntitySnapshotDto(resourceChanged.Entity)),
            CooldownStartedEvent cooldownStarted => new BattleEventDto(
                Type: BattleEventType.CooldownStarted,
                Tick: cooldownStarted.Tick,
                Entity: ToEntitySnapshotDto(cooldownStarted.Entity),
                SkillId: cooldownStarted.SkillId,
                Amount: cooldownStarted.DurationSeconds),
            CooldownReadyEvent cooldownReady => new BattleEventDto(
                Type: BattleEventType.CooldownReady,
                Tick: cooldownReady.Tick,
                Entity: ToEntitySnapshotDto(cooldownReady.Entity),
                SkillId: cooldownReady.SkillId),
            _ => new BattleEventDto(
                Type: BattleEventType.Unknown,
                Tick: battleEvent.Tick)
        };
    }
}
