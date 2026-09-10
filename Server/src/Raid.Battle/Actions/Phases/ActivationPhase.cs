using System.Numerics;
using Raid.Battle.Combat;
using Raid.Battle.Entities;
using Raid.Battle.Movement;
using Raid.Battle.World;
using Raid.Contracts.Common;

namespace Raid.Battle.Actions.Phases;

public sealed class ActivationPhase : IActionPhase
{
    public ActionPhaseKind Kind => ActionPhaseKind.Activation;

    public void Enter(ActionContext context)
    {
        if (context.Owner is PlayerEntity player
            && !context.Action.Skill.CommitsOnStart)
        {
            var skill = context.Action.Skill;
            if (!context.World.Chains.DefersCooldown(player, skill))
            {
                context.World.Cooldowns.StartCooldown(
                    player,
                    skill.SkillId,
                    skill.CooldownMilliseconds);
            }

            if (skill.ManaCost > 0
                && !context.World.Resources.TrySpendMana(player, skill.ManaCost))
            {
                return;
            }

            context.World.Chains.Begin(player, skill);
        }

        if (context.Action.Skill.Kind == SkillKind.Toggle
            && context.Owner is PlayerEntity toggleOwner)
        {
            EffectToggle.Apply(context.World, toggleOwner, context.Action.Skill);
            return;
        }

        ApplyOnActivationEffects(context);
        ApplyHits(context);
        ApplyDash(context);
    }

    public ActionPhaseResult Update(ActionContext context, float deltaTime)
    {
        return ActionPhaseResult.Advance;
    }

    public void Exit(ActionContext context, ActionEndReason reason)
    {
    }

    private static void ApplyOnActivationEffects(ActionContext context)
    {
        var target = context.TargetId is EntityId targetId
            ? context.World.Entities.Find(targetId)
            : context.Owner;

        SkillEffectApplier.Apply(
            context.World,
            SkillEffectTrigger.OnActivation,
            context.Action.Skill,
            context.Owner,
            target,
            context.Target.Position);
    }

    private static void ApplyDash(ActionContext context)
    {
        var skill = context.Action.Skill;
        if (skill.DashDistance <= 0f)
        {
            return;
        }

        var direction = ResolveDashDirection(context);
        if (direction is null)
        {
            return;
        }

        var destination = context.Owner.Position + (direction.Value * skill.DashDistance);
        if (skill.InstantDash)
        {
            context.World.Movement.SetPosition(new PositionSetRequest(
                context.Owner.Id,
                destination,
                PositionSetReason.SkillDash));
            return;
        }

        if (skill.DashSpeed <= 0f)
        {
            throw new InvalidOperationException(
                $"Skill '{skill.SkillId}' requires dashSpeed.");
        }

        context.World.Movement.SetIntent(new MovementIntent(
            context.Owner.Id,
            destination,
            direction.Value,
            skill.DashSpeed,
            context.Owner.TurnSpeedRadiansPerSecond,
            MovementFacingPolicy.RotateWhileMoving));
    }

    private static Vector2? ResolveDashDirection(ActionContext context)
    {
        if (context.Target.Direction is Vector2 targetDirection && targetDirection != Vector2.Zero)
        {
            return Vector2.Normalize(targetDirection);
        }

        if (context.Target.Position is Vector2 point)
        {
            var toPoint = point - context.Owner.Position;
            if (toPoint != Vector2.Zero)
            {
                return Vector2.Normalize(toPoint);
            }
        }

        return context.Owner.FacingDirection == Vector2.Zero
            ? null
            : Vector2.Normalize(context.Owner.FacingDirection);
    }

    private static void ApplyHits(ActionContext context)
    {
        var skill = context.Action.Skill;
        var damage = skill.Kind == SkillKind.Charge
            ? context.Action.ScaledDamage
            : context.Action.Damage;

        if (skill.Kind == SkillKind.Hold)
        {
            context.World.Combat.EnqueueAreaHits(
                context.Owner.Id,
                context.Owner.Position,
                skill.Width,
                skill.SkillId,
                damage);
            return;
        }

        if (damage <= 0f)
        {
            return;
        }

        if (skill.TargetingMode == SkillTargetingMode.Point)
        {
            SchedulePointImpact(context, skill, damage);
            return;
        }

        if (skill.TargetingMode == SkillTargetingMode.Direction)
        {
            ApplyDirectionImpact(context, skill, damage);
            return;
        }

        if (context.TargetId is null)
        {
            return;
        }

        if (skill.Kind == SkillKind.Charge)
        {
            var target = context.World.Entities.Find(context.TargetId.Value);
            if (target is null)
            {
                return;
            }

            var distance = Vector2.Distance(context.Owner.Position, target.Position)
                - target.CollisionRadius;
            if (distance > context.Action.ScaledRange)
            {
                return;
            }
        }

        var request = new HitRequest(
            context.Owner.Id,
            context.TargetId.Value,
            context.Action.SkillId,
            damage);
        context.World.DelayedHits.Schedule(request, ResolveEntityDelayTicks(context, skill));
    }

    private static void SchedulePointImpact(
        ActionContext context,
        SkillDefinition skill,
        float damage)
    {
        if (context.Target.Position is not Vector2 point || skill.Width <= 0f)
        {
            return;
        }

        context.World.DelayedHits.ScheduleArea(
            context.Owner.Id,
            point,
            skill.Width,
            context.Action.SkillId,
            damage,
            ResolvePointDelayTicks(context, skill));
    }

    private static void ApplyDirectionImpact(
        ActionContext context,
        SkillDefinition skill,
        float damage)
    {
        if (context.Target.Direction is not Vector2 direction || direction == Vector2.Zero)
        {
            return;
        }

        if (skill.HasProjectile)
        {
            context.World.Projectiles.SpawnDirection(
                context.Owner.Id,
                context.Owner.Position,
                direction,
                skill.Range,
                skill.Width * 0.5f,
                skill.ProjectileSpeed,
                skill.SkillId,
                damage);
            return;
        }

        context.World.Combat.EnqueueLineHits(
            context.Owner.Id,
            context.Owner.Position,
            direction,
            skill.Range,
            skill.Width,
            skill.SkillId,
            damage);
    }

    private static int ResolveEntityDelayTicks(ActionContext context, SkillDefinition skill)
    {
        if (!skill.HasProjectile || context.TargetId is null)
        {
            return 0;
        }

        var target = context.World.Entities.Find(context.TargetId.Value);
        if (target is null)
        {
            return 0;
        }

        var distance = Vector2.Distance(context.Owner.Position, target.Position)
            - target.CollisionRadius;
        return context.World.Settings.ToTravelTicks(distance, skill.ProjectileSpeed);
    }

    private static int ResolvePointDelayTicks(ActionContext context, SkillDefinition skill)
    {
        if (!skill.HasProjectile)
        {
            return 0;
        }

        return context.World.Settings.ToTravelTicks(
            WorldSettings.PointProjectileTravelDistance,
            skill.ProjectileSpeed);
    }
}
