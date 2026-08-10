namespace Raid.Battle.Combat;

public sealed class PlayerClassDefinition(
    string classId,
    string displayName,
    IReadOnlyList<SkillDefinition> skills)
{
    public string ClassId { get; } = string.IsNullOrWhiteSpace(classId)
        ? throw new ArgumentException("Class id is required.", nameof(classId))
        : classId;

    public string DisplayName { get; } = string.IsNullOrWhiteSpace(displayName)
        ? throw new ArgumentException("Display name is required.", nameof(displayName))
        : displayName;

    public IReadOnlyList<SkillDefinition> Skills { get; } = skills
        ?? throw new ArgumentNullException(nameof(skills));

    public SkillDefinition? FindSkill(string skillId)
    {
        return Skills.FirstOrDefault(skill => skill.SkillId == skillId);
    }
}
