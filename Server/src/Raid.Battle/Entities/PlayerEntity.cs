using System.Numerics;
using Raid.Battle.Combat;

namespace Raid.Battle.Entities;

public sealed class PlayerEntity(
    EntityId id,
    string userId,
    int participantSlot,
    PlayerClassDefinition playerClass,
    Vector2 position,
    float moveSpeed = 6f,
    float maxHealth = 1000f)
    : BattleEntity(id, EntityKind.Player, position, maxHealth)
{
    public string UserId { get; } = userId;

    public int ParticipantSlot { get; } = participantSlot;

    public PlayerClassDefinition Class { get; } = playerClass;

    public float MoveSpeed { get; } = moveSpeed;

    public SkillDefinition? FindSkill(string skillId)
    {
        return Class.FindSkill(skillId);
    }
}
