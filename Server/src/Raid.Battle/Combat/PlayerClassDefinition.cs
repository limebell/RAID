namespace Raid.Battle.Combat;

public sealed class PlayerClassDefinition(
    string classId,
    string displayName,
    float maxHealth,
    float maxMana,
    float manaRegenPerSecond,
    float moveSpeed,
    float turnSpeedRadiansPerSecond,
    IReadOnlyList<SkillDefinition> skills)
{
    public string ClassId { get; } = string.IsNullOrWhiteSpace(classId)
        ? throw new ArgumentException("Class id is required.", nameof(classId))
        : classId;

    public string DisplayName { get; } = string.IsNullOrWhiteSpace(displayName)
        ? throw new ArgumentException("Display name is required.", nameof(displayName))
        : displayName;

    public float MaxHealth { get; } = maxHealth;

    public float MaxMana { get; } = maxMana;

    public float ManaRegenPerSecond { get; } = manaRegenPerSecond;

    public float MoveSpeed { get; } = moveSpeed;

    public float TurnSpeedRadiansPerSecond { get; } = turnSpeedRadiansPerSecond;

    public IReadOnlyList<SkillDefinition> Skills { get; } = skills
        ?? throw new ArgumentNullException(nameof(skills));

    public SkillDefinition? FindSkill(string skillId)
    {
        return Skills.FirstOrDefault(skill => skill.SkillId == skillId);
    }

    public bool IsChainFollowUp(string skillId)
    {
        return Skills.Any(skill =>
            string.Equals(skill.ChainToSkillId, skillId, StringComparison.Ordinal));
    }
}
