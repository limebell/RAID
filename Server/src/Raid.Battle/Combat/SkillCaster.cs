using System.Numerics;
using Raid.Battle.Commands;
using Raid.Battle.Entities;
using Raid.Battle.World;
using Raid.Contracts.Common;

namespace Raid.Battle.Combat;

internal static class SkillCaster
{
    public static bool IsInRange(BattleEntity caster, BattleEntity target, float range)
    {
        var distanceToCollisionEdge =
            Vector2.Distance(caster.Position, target.Position) - target.CollisionRadius;
        return distanceToCollisionEdge <= range;
    }

    public static bool IsLivingHostile(BattleEntity attacker, BattleEntity other)
    {
        if (other.Id == attacker.Id || other.IsDowned || other.CurrentHealth <= 0f)
        {
            return false;
        }

        return other.Kind is not EntityKind.Player;
    }

    public static bool IsLivingAlly(BattleEntity other)
    {
        return other.Kind == EntityKind.Player
            && !other.IsDowned
            && other.CurrentHealth > 0f;
    }

    public static bool TryStart(
        BattleWorld world,
        PlayerEntity player,
        SkillDefinition skill,
        SkillTarget? target)
    {
        if (player.Actions.IsBusy)
        {
            return false;
        }

        if (!world.Cooldowns.CanUseSkill(player, skill.SkillId))
        {
            return false;
        }

        world.Chains.OnSkillStarting(player, skill);
        var resolved = SkillResolver.Resolve(world, player, skill);

        var resolvedTarget = target ?? new SkillTarget(SkillTargetingMode.None);

        var action = SkillActionFactory.Create(
            world.Actions.NextActionId(),
            player.Id,
            resolved,
            resolvedTarget,
            world.Settings);

        if (!world.Actions.TryStart(player, action))
        {
            return false;
        }

        TryTurnFacing(world, player, resolved, resolvedTarget);
        return true;
    }

    public static void MoveToward(BattleWorld world, PlayerEntity player, Vector2 destination)
    {
        if (player.Actions.LocksMovement)
        {
            return;
        }

        world.Movement.MoveTo(player, destination);
    }

    public static void StopIfMoving(BattleWorld world, PlayerEntity player)
    {
        if (player.ActiveMovement is not null)
        {
            world.Movement.ClearIntent(player.Id);
        }
    }

    private static void TryTurnFacing(
        BattleWorld world,
        PlayerEntity player,
        SkillDefinition skill,
        SkillTarget target)
    {
        if (skill.DashDistance > 0f && !skill.InstantDash)
        {
            return;
        }

        var facing = ResolveFacingDirection(world, player, target);
        if (facing is null)
        {
            return;
        }

        world.Movement.SetDirectionIntent(player.Id, facing.Value);
    }

    private static Vector2? ResolveFacingDirection(
        BattleWorld world,
        PlayerEntity player,
        SkillTarget target)
    {
        switch (target.Mode)
        {
            case SkillTargetingMode.Entity:
            {
                if (target.EntityId is null)
                {
                    return null;
                }

                var entity = world.Entities.Find(target.EntityId.Value);
                if (entity is null)
                {
                    return null;
                }

                return NonZeroOrNull(entity.Position - player.Position);
            }

            case SkillTargetingMode.Point:
                return target.Position is Vector2 point
                    ? NonZeroOrNull(point - player.Position)
                    : null;

            case SkillTargetingMode.Direction:
                return target.Direction is Vector2 direction
                    ? NonZeroOrNull(direction)
                    : null;

            default:
                return null;
        }
    }

    private static Vector2? NonZeroOrNull(Vector2 vector)
    {
        return vector == Vector2.Zero ? null : vector;
    }
}
