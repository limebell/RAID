using Raid.Battle.Entities;
using Raid.Battle.World;

namespace Raid.Battle.Combat;

public static class SkillResolver
{
    public static SkillDefinition Resolve(BattleWorld world, PlayerEntity player, SkillDefinition skill)
    {
        if (string.IsNullOrWhiteSpace(skill.VariantGroup))
        {
            return skill;
        }

        return FindActiveVariant(world, player, skill.VariantGroup) ?? skill;
    }

    public static SkillDefinition? FindBasicAttack(BattleWorld world, PlayerEntity player)
    {
        SkillDefinition? fallback = null;
        foreach (var skill in player.Class.Skills)
        {
            if (!skill.IsBasicAttack)
            {
                continue;
            }

            fallback ??= skill;
            if (IsAvailable(world, player, skill))
            {
                return skill;
            }
        }

        return fallback;
    }

    public static bool IsUnavailable(BattleWorld world, PlayerEntity player, SkillDefinition skill)
    {
        return !string.IsNullOrWhiteSpace(skill.RequiredBuffId)
            && !world.Effects.HasBuff(player.Id, skill.RequiredBuffId);
    }

    private static SkillDefinition? FindActiveVariant(
        BattleWorld world,
        PlayerEntity player,
        string variantGroup)
    {
        foreach (var skill in player.Class.Skills)
        {
            if (!string.Equals(skill.VariantGroup, variantGroup, StringComparison.Ordinal))
            {
                continue;
            }

            if (IsAvailable(world, player, skill))
            {
                return skill;
            }
        }

        return null;
    }

    private static bool IsAvailable(BattleWorld world, PlayerEntity player, SkillDefinition skill)
    {
        return string.IsNullOrWhiteSpace(skill.RequiredBuffId)
            || world.Effects.HasBuff(player.Id, skill.RequiredBuffId);
    }
}
