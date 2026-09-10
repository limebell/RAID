using Raid.Battle.Effects;
using Raid.Battle.Entities;
using Raid.Battle.World;
using Raid.Contracts.Common;

namespace Raid.Battle.Combat;

public static class EffectToggle
{
    public static void ApplyDefaultOff(BattleWorld world, PlayerEntity player)
    {
        foreach (var skill in player.Class.Skills)
        {
            if (!skill.IsBuffToggle || world.Effects.HasBuff(player.Id, skill.ToggleOnBuffId!))
            {
                continue;
            }

            ApplyBuff(world, player, skill, skill.ToggleOffBuffId!);
        }
    }

    public static void Apply(BattleWorld world, PlayerEntity player, SkillDefinition skill)
    {
        if (!skill.IsBuffToggle)
        {
            return;
        }

        if (world.Effects.HasBuff(player.Id, skill.ToggleOnBuffId!))
        {
            world.Effects.RemoveBuff(player.Id, skill.ToggleOnBuffId!);
            ApplyBuff(world, player, skill, skill.ToggleOffBuffId!);
            return;
        }

        world.Effects.RemoveBuff(player.Id, skill.ToggleOffBuffId!);
        ApplyBuff(world, player, skill, skill.ToggleOnBuffId!);
    }

    private static void ApplyBuff(
        BattleWorld world,
        PlayerEntity player,
        SkillDefinition skill,
        string buffId)
    {
        world.Effects.Apply(new StatusEffect(
            player.Id,
            StatusEffectKind.Buff,
            player.Id,
            skill.SkillId,
            float.PositiveInfinity,
            buffId: buffId));
    }
}
