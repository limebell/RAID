using Raid.Contracts.Battle.Commands;
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
/// <see cref="BattleEventType.HealApplied"/> — heal target (includes updated health).
/// <see cref="BattleEventType.ShieldChanged"/> — entity whose shield changed.
/// <see cref="BattleEventType.StatusEffectApplied"/>,
/// <see cref="BattleEventType.StatusEffectRemoved"/> — effect target.
/// <see cref="BattleEventType.ResourceChanged"/> — entity whose mana changed from natural regen.
/// </param>
/// <param name="AttackerId">
/// Attacker or source entity id. Used by <see cref="BattleEventType.DamageApplied"/>,
/// <see cref="BattleEventType.HealApplied"/>, and
/// <see cref="BattleEventType.StatusEffectApplied"/>; the target is <paramref name="Entity"/>.
/// </param>
/// <param name="SkillId">
/// Skill identifier. Used by
/// <see cref="BattleEventType.ActionStarted"/>,
/// <see cref="BattleEventType.ActionPhaseChanged"/>,
/// <see cref="BattleEventType.ActionEnded"/>, and
/// <see cref="BattleEventType.DamageApplied"/>,
/// <see cref="BattleEventType.HealApplied"/>,
/// <see cref="BattleEventType.ShieldChanged"/>,
/// <see cref="BattleEventType.StatusEffectApplied"/>,
/// <see cref="BattleEventType.StatusEffectRemoved"/>,
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
/// <see cref="BattleEventType.PositionSet"/> (spawn, reset, …),
/// <see cref="BattleEventType.ActionEnded"/> (completed, cancelled, …),
/// <see cref="BattleEventType.StatusEffectApplied"/>, and
/// <see cref="BattleEventType.StatusEffectRemoved"/> (effect kind).
/// </param>
/// <param name="Amount">
/// Damage dealt for <see cref="BattleEventType.DamageApplied"/>;
/// heal amount for <see cref="BattleEventType.HealApplied"/>;
/// current shield for <see cref="BattleEventType.ShieldChanged"/>;
/// remaining duration in seconds for <see cref="BattleEventType.StatusEffectApplied"/>;
/// cooldown duration in seconds for <see cref="BattleEventType.CooldownStarted"/>;
/// phase duration in seconds for <see cref="BattleEventType.ActionStarted"/> and
/// <see cref="BattleEventType.ActionPhaseChanged"/>.
/// </param>
/// <param name="Target">
/// Skill target for <see cref="BattleEventType.ActionStarted"/> and
/// <see cref="BattleEventType.ActionPhaseChanged"/>. Includes entity, point, or direction
/// according to the skill targeting mode.
/// </param>
public sealed record BattleEventDto(
    BattleEventType Type,
    long Tick,
    EntitySnapshotDto? Entity = null,
    long? AttackerId = null,
    string? SkillId = null,
    string? Phase = null,
    string? Reason = null,
    float? Amount = null,
    SkillTargetDto? Target = null);
