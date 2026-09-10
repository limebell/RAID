using System.Numerics;
using Raid.Battle.Combat;
using Raid.Contracts.Common;

namespace Raid.Battle.Entities;

public sealed class PlayerEntity(
    EntityId id,
    string userId,
    int participantSlot,
    PlayerClassDefinition playerClass,
    Vector2 position,
    Vector2 facingDirection,
    float moveSpeed,
    float turnSpeedRadiansPerSecond)
    : BattleEntity(
        id,
        EntityKind.Player,
        position,
        facingDirection,
        moveSpeed,
        turnSpeedRadiansPerSecond,
        playerClass.MaxHealth)
{
    public string UserId { get; } = userId;

    public int ParticipantSlot { get; } = participantSlot;

    public PlayerClassDefinition Class { get; } = playerClass;

    public SkillBar SkillBar { get; internal set; } = SkillBar.Empty;

    public override string DefinitionId => Class.ClassId;

    public float MaxMana { get; } = playerClass.MaxMana;

    public float CurrentMana { get; internal set; } = playerClass.MaxMana;

    public float ManaRegenPerSecond { get; } = playerClass.ManaRegenPerSecond;

    internal Dictionary<string, float> SkillCooldownRemainingSeconds { get; } =
        new(StringComparer.Ordinal);

    internal CombatOrder CombatOrder { get; } = new();

    internal SkillChainState? ChainState { get; set; }

    public SkillDefinition? FindSkill(string skillId)
    {
        return Class.FindSkill(skillId);
    }
}
