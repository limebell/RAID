using System.Numerics;
using Raid.Battle.Commands;
using Raid.Battle.Entities;
using Raid.Battle.World;
using Raid.Contracts.Common;

namespace Raid.Battle.Combat;

public sealed class CombatOrderSystem(BattleWorld world)
{
    public void Update()
    {
        foreach (var player in world.Entities.Players())
        {
            Process(player);
        }
    }

    private void Process(PlayerEntity player)
    {
        if (player.IsDowned || player.CurrentHealth <= 0f)
        {
            player.CombatOrder.Clear();
            return;
        }

        switch (player.CombatOrder.Kind)
        {
            case CombatOrderKind.AttackTarget:
                ProcessAttackTarget(player);
                break;
            case CombatOrderKind.AttackMove:
                ProcessAttackMove(player);
                break;
            case CombatOrderKind.ChaseCast:
                ProcessChaseCast(player);
                break;
        }
    }

    private void ProcessAttackTarget(PlayerEntity player)
    {
        var target = ResolveLivingTarget(player.CombatOrder.TargetId);
        if (target is null)
        {
            player.CombatOrder.Clear();
            SkillCaster.StopIfMoving(world, player);
            return;
        }

        var skill = SkillResolver.FindBasicAttack(world, player);
        if (skill is null)
        {
            Pursue(player, target.Position);
            return;
        }

        skill = SkillResolver.Resolve(world, player, skill);

        PursueOrCast(
            player,
            target,
            skill,
            new SkillTarget(SkillTargetingMode.Entity, EntityId: target.Id),
            clearOrderOnCast: false);
    }

    private void ProcessAttackMove(PlayerEntity player)
    {
        var skill = SkillResolver.FindBasicAttack(world, player);
        if (skill is not null)
        {
            skill = SkillResolver.Resolve(world, player, skill);
            var enemy = FindNearestHostileInRange(player, skill.Range);
            if (enemy is not null)
            {
                PursueOrCast(
                    player,
                    enemy,
                    skill,
                    new SkillTarget(SkillTargetingMode.Entity, EntityId: enemy.Id),
                    clearOrderOnCast: false);
                return;
            }
        }

        var destination = player.CombatOrder.Destination!.Value;
        if (HasArrived(player, destination))
        {
            player.CombatOrder.Clear();
            return;
        }

        Pursue(player, destination);
    }

    private void ProcessChaseCast(PlayerEntity player)
    {
        var order = player.CombatOrder;
        var skill = order.Skill;
        var skillTarget = order.SkillTarget;
        if (skill is null || skillTarget?.EntityId is null)
        {
            order.Clear();
            return;
        }

        skill = SkillResolver.Resolve(world, player, skill);

        var target = ResolveLivingTarget(skillTarget.EntityId);
        if (target is null)
        {
            order.Clear();
            SkillCaster.StopIfMoving(world, player);
            return;
        }

        PursueOrCast(player, target, skill, skillTarget, clearOrderOnCast: true);
    }

    private void PursueOrCast(
        PlayerEntity player,
        BattleEntity target,
        SkillDefinition skill,
        SkillTarget skillTarget,
        bool clearOrderOnCast)
    {
        if (player.Actions.IsBusy)
        {
            return;
        }

        if (SkillCaster.IsInRange(player, target, skill.Range))
        {
            SkillCaster.StopIfMoving(world, player);
            if (SkillCaster.TryStart(world, player, skill, skillTarget) && clearOrderOnCast)
            {
                player.CombatOrder.Clear();
            }

            return;
        }

        SkillCaster.MoveToward(world, player, target.Position);
    }

    private void Pursue(PlayerEntity player, Vector2 destination)
    {
        if (player.Actions.IsBusy)
        {
            return;
        }

        SkillCaster.MoveToward(world, player, destination);
    }

    private BattleEntity? ResolveLivingTarget(EntityId? targetId)
    {
        if (targetId is null)
        {
            return null;
        }

        var entity = world.Entities.Find(targetId.Value);
        if (entity is null || entity.IsDowned || entity.CurrentHealth <= 0f)
        {
            return null;
        }

        return entity;
    }

    private BattleEntity? FindNearestHostileInRange(PlayerEntity player, float range)
    {
        BattleEntity? nearest = null;
        var nearestDistance = float.MaxValue;

        foreach (var entity in world.Entities.All())
        {
            if (!SkillCaster.IsLivingHostile(player, entity)
                || !SkillCaster.IsInRange(player, entity, range))
            {
                continue;
            }

            var distance = Vector2.Distance(player.Position, entity.Position);
            if (distance >= nearestDistance)
            {
                continue;
            }

            nearest = entity;
            nearestDistance = distance;
        }

        return nearest;
    }

    private static bool HasArrived(PlayerEntity player, Vector2 destination)
    {
        return player.ActiveMovement is null
            && Vector2.Distance(player.Position, destination) <= 0.05f;
    }
}
