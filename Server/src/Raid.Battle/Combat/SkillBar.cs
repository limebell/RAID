using Raid.Contracts.Common;

namespace Raid.Battle.Combat;

public sealed class SkillBar
{
    public const int SlotCount = 6;

    public static SkillBar Empty { get; } = new([]);

    private SkillBar(IReadOnlyList<string> skillIds)
    {
        SkillIds = skillIds;
    }

    public IReadOnlyList<string> SkillIds { get; }

    public static SkillBar Create(
        PlayerClassDefinition playerClass,
        IReadOnlyList<string>? skillIds)
    {
        ArgumentNullException.ThrowIfNull(playerClass);

        if (skillIds is null || skillIds.Count == 0)
        {
            return Empty;
        }

        if (skillIds.Count > SlotCount)
        {
            throw new ArgumentException(
                $"A skill bar can have at most {SlotCount} skills.",
                nameof(skillIds));
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var normalized = new string[skillIds.Count];
        for (var i = 0; i < skillIds.Count; i++)
        {
            var skillId = skillIds[i];
            if (string.IsNullOrWhiteSpace(skillId))
            {
                throw new ArgumentException("Bar skill id is required.", nameof(skillIds));
            }

            if (!seen.Add(skillId))
            {
                throw new ArgumentException($"Duplicate bar skill '{skillId}'.", nameof(skillIds));
            }

            if (playerClass.FindSkill(skillId) is null)
            {
                throw new ArgumentException($"Unknown bar skill '{skillId}'.", nameof(skillIds));
            }

            normalized[i] = skillId;
        }

        return new SkillBar(normalized);
    }

    public SkillBarSlot? GetSlot(string skillId)
    {
        for (var i = 0; i < SkillIds.Count; i++)
        {
            if (string.Equals(SkillIds[i], skillId, StringComparison.Ordinal))
            {
                return (SkillBarSlot)i;
            }
        }

        return null;
    }
}
