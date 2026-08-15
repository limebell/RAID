using Raid.Contracts.Battle.Snapshots;

namespace Raid.Contracts.Battle.Events;

/// <summary>
/// Tick delta event envelope. Optional fields depend on <see cref="Type"/>.
/// Entity state is carried in <paramref name="Entity"/>; relationship and action metadata use the other fields.
/// </summary>
/// <param name="Type">Event kind. Determines which optional fields are populated.</param>
/// <param name="Tick">World tick when the event occurred.</param>
/// <param name="Entity">
/// Entity state after this event.
/// <see cref="BattleEventType.EntitySpawned"/> — spawned entity;
/// <see cref="BattleEventType.EntityMoved"/> — moved entity;
/// <see cref="BattleEventType.EntityMoveCompleted"/> — entity that finished moving;
/// <see cref="BattleEventType.PositionSet"/> — entity whose position was corrected;
/// <see cref="BattleEventType.ActionStarted"/>,
/// <see cref="BattleEventType.ActionPhaseChanged"/>,
/// <see cref="BattleEventType.ActionEnded"/> — action owner (caster);
/// <see cref="BattleEventType.DamageApplied"/> — damage target (includes updated health).
/// <see cref="BattleEventType.ResourceChanged"/> — entity whose mana changed from natural regen.
/// </param>
/// <param name="AttackerId">
/// Attacker entity id. Used by <see cref="BattleEventType.DamageApplied"/> only; the target is <paramref name="Entity"/>.
/// </param>
/// <param name="SkillId">
/// Skill identifier. Used by
/// <see cref="BattleEventType.ActionStarted"/>,
/// <see cref="BattleEventType.ActionPhaseChanged"/>,
/// <see cref="BattleEventType.ActionEnded"/>, and
/// <see cref="BattleEventType.DamageApplied"/>,
/// <see cref="BattleEventType.CooldownStarted"/>, and
/// <see cref="BattleEventType.CooldownReady"/>.
/// </param>
/// <param name="Phase">
/// Current action phase name. Used by
/// <see cref="BattleEventType.ActionStarted"/> and
/// <see cref="BattleEventType.ActionPhaseChanged"/>.
/// </param>
/// <param name="Reason">
/// Event-specific reason code as a string. Used by
/// <see cref="BattleEventType.PositionSet"/> (spawn, reset, …) and
/// <see cref="BattleEventType.ActionEnded"/> (completed, cancelled, …).
/// </param>
/// <param name="Amount">
/// Damage dealt for <see cref="BattleEventType.DamageApplied"/>;
/// cooldown duration in seconds for <see cref="BattleEventType.CooldownStarted"/>.
/// </param>
public sealed record BattleEventDto(
    BattleEventType Type,
    long Tick,
    EntitySnapshotDto? Entity = null,
    long? AttackerId = null,
    string? SkillId = null,
    string? Phase = null,
    string? Reason = null,
    float? Amount = null);
