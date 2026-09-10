using Raid.Battle.Commands;
using Raid.Battle.Entities;
using Raid.Battle.Events;
using Raid.Battle.Snapshots;
using Raid.Battle.World;
using Raid.Contracts.Battle.Commands;
using Raid.Contracts.Battle.Events;
using Raid.Contracts.Battle.Snapshots;
using Raid.Contracts.Session;
using Raid.Contracts.Common;
using System.Numerics;

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
            snapshot.DefinitionId,
            new Vector2Dto(snapshot.Position.X, snapshot.Position.Y),
            new Vector2Dto(snapshot.FacingDirection.X, snapshot.FacingDirection.Y),
            snapshot.CurrentHealth,
            snapshot.MaxHealth,
            snapshot.CurrentMana,
            snapshot.MaxMana,
            snapshot.IsBusy,
            snapshot.CurrentPhase,
            snapshot.CurrentShield);
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
                Phase: started.Phase?.ToString(),
                Amount: started.DurationSeconds,
                Target: ToSkillTargetDto(started.Target)),
            ActionPhaseChangedEvent phaseChanged => new BattleEventDto(
                Type: BattleEventType.ActionPhaseChanged,
                Tick: phaseChanged.Tick,
                Entity: ToEntitySnapshotDto(phaseChanged.Entity),
                SkillId: phaseChanged.SkillId,
                Phase: phaseChanged.Phase.ToString(),
                Amount: phaseChanged.DurationSeconds,
                Target: ToSkillTargetDto(phaseChanged.Target)),
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
            HealAppliedEvent heal => new BattleEventDto(
                Type: BattleEventType.HealApplied,
                Tick: heal.Tick,
                Entity: ToEntitySnapshotDto(heal.Target),
                AttackerId: heal.SourceId.Value,
                SkillId: heal.SkillId,
                Amount: heal.Amount),
            ShieldChangedEvent shield => new BattleEventDto(
                Type: BattleEventType.ShieldChanged,
                Tick: shield.Tick,
                Entity: ToEntitySnapshotDto(shield.Target),
                SkillId: shield.SkillId,
                Amount: shield.Amount),
            StatusEffectAppliedEvent statusApplied => new BattleEventDto(
                Type: BattleEventType.StatusEffectApplied,
                Tick: statusApplied.Tick,
                Entity: ToEntitySnapshotDto(statusApplied.Target),
                AttackerId: statusApplied.SourceId?.Value,
                SkillId: statusApplied.SkillId,
                Reason: statusApplied.BuffId ?? statusApplied.Kind.ToString(),
                Amount: float.IsFinite(statusApplied.DurationSeconds)
                    ? statusApplied.DurationSeconds
                    : -1f),
            StatusEffectRemovedEvent statusRemoved => new BattleEventDto(
                Type: BattleEventType.StatusEffectRemoved,
                Tick: statusRemoved.Tick,
                Entity: ToEntitySnapshotDto(statusRemoved.Target),
                SkillId: statusRemoved.SkillId,
                Reason: statusRemoved.BuffId ?? statusRemoved.Kind.ToString()),
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
            ZoneSpawnedEvent zoneSpawned => new BattleEventDto(
                Type: BattleEventType.ZoneSpawned,
                Tick: zoneSpawned.Tick,
                Entity: ToEntitySnapshotDto(zoneSpawned.Owner),
                SkillId: zoneSpawned.SkillId,
                Amount: zoneSpawned.Radius,
                Target: new SkillTargetDto(
                    SkillTargetingMode.Point,
                    Position: ToVector2Dto(zoneSpawned.Position))),
            ZoneExpiredEvent zoneExpired => new BattleEventDto(
                Type: BattleEventType.ZoneExpired,
                Tick: zoneExpired.Tick,
                Entity: ToEntitySnapshotDto(zoneExpired.Owner),
                SkillId: zoneExpired.SkillId,
                Target: new SkillTargetDto(
                    SkillTargetingMode.Point,
                    Position: ToVector2Dto(zoneExpired.Position))),
            _ => new BattleEventDto(
                Type: BattleEventType.Unknown,
                Tick: battleEvent.Tick)
        };
    }

    private static SkillTargetDto? ToSkillTargetDto(SkillTarget? target)
    {
        if (target is null)
        {
            return null;
        }

        return new SkillTargetDto(
            target.Mode,
            target.EntityId?.Value,
            ToVector2Dto(target.Position),
            ToVector2Dto(target.Direction));
    }

    private static Vector2Dto? ToVector2Dto(Vector2? value)
    {
        return value is Vector2 vector ? new Vector2Dto(vector.X, vector.Y) : null;
    }
}
