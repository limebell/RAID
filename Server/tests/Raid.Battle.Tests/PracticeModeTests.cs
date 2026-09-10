using System.Numerics;
using Raid.Battle.Combat;
using Raid.Battle.Commands;
using Raid.Battle.Entities;
using Raid.Battle.Events;
using Raid.Battle.Movement;
using Raid.Battle.World;
using Raid.Contracts.Common;

namespace Raid.Battle.Tests;

public sealed class PracticeModeTests
{
    [Fact]
    public void InstantSkill_DamagesDummy()
    {
        var (world, player) = CreatePracticeWithPlayer(new WorldSettings
        {
            FixedDeltaMilliseconds = 50
        });

        var skill = player.FindSkill("test.instant_strike")!;
        var dummy = world.Entities.Dummies().Single();
        var healthBefore = dummy.CurrentHealth;
        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(
                Mode: SkillTargetingMode.Entity,
                EntityId: dummy.Id)));

        for (var i = 0; i < 20; i++)
        {
            world.Loop.Tick();
            if (dummy.CurrentHealth < healthBefore)
            {
                break;
            }
        }

        Assert.Equal(healthBefore - skill.Damage, dummy.CurrentHealth);

        var events = world.Events.Drain();
        Assert.Contains(events, e => e is DamageAppliedEvent damage
            && damage.Target.EntityId == dummy.Id
            && damage.Amount == skill.Damage);
    }

    [Fact]
    public void EntitySkill_AllowsTarget_WhenCollisionEdgeIsInRange()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.instant_strike")!;
        var dummy = world.Entities.Dummies().Single();
        world.Movement.SetPosition(new PositionSetRequest(
            dummy.Id,
            new Vector2(skill.Range + (dummy.CollisionRadius * 0.5f), 0f),
            PositionSetReason.Reset));

        var result = new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id))
            .Execute(new CommandContext(world));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void EntitySkill_ChasesThenCasts_WhenCollisionEdgeIsOutOfRange()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.instant_strike")!;
        var dummy = world.Entities.Dummies().Single();
        var healthBefore = dummy.CurrentHealth;
        world.Movement.SetPosition(new PositionSetRequest(
            dummy.Id,
            new Vector2(skill.Range + dummy.CollisionRadius + 4f, 0f),
            PositionSetReason.Reset));

        var result = new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id))
            .Execute(new CommandContext(world));

        Assert.True(result.Succeeded);
        Assert.Equal(CombatOrderKind.ChaseCast, player.CombatOrder.Kind);

        for (var i = 0; i < 120; i++)
        {
            world.Loop.Tick();
            if (dummy.CurrentHealth < healthBefore)
            {
                break;
            }
        }

        Assert.Equal(healthBefore - skill.Damage, dummy.CurrentHealth);
        Assert.False(player.CombatOrder.IsActive);
    }

    [Fact]
    public void CastSkill_AppliesDamage_AfterCastAndWindupComplete()
    {
        var (world, player) = CreatePracticeWithPlayer(new WorldSettings
        {
            FixedDeltaMilliseconds = 500
        });

        var skill = player.FindSkill("test.meteor_strike")!;
        var dummy = world.Entities.Dummies().Single();
        var healthBefore = dummy.CurrentHealth;
        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            PointTargetAt(dummy)));

        world.Loop.Tick();
        Assert.Equal(healthBefore, dummy.CurrentHealth);
        Assert.True(player.Actions.IsCasting);

        world.Loop.Tick();
        Assert.Equal(healthBefore, dummy.CurrentHealth);
        Assert.Equal(Raid.Battle.Actions.ActionPhaseKind.Windup, player.Actions.CurrentAction?.CurrentPhaseKind);

        world.Loop.Tick();
        Assert.Equal(healthBefore, dummy.CurrentHealth);
        Assert.False(player.Actions.IsCasting);
    }

    [Fact]
    public void Move_CancelsCasting()
    {
        var (world, player) = CreatePracticeWithPlayer(new WorldSettings
        {
            FixedDeltaMilliseconds = 100
        });

        var skill = player.FindSkill("test.meteor_strike")!;
        var dummy = world.Entities.Dummies().Single();
        var healthBefore = dummy.CurrentHealth;
        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            PointTargetAt(dummy)));
        world.Loop.Tick();

        world.Commands.Enqueue(new MoveCommand(player.Id, new Vector2(1f, 0f)));
        world.Loop.Tick();

        Assert.False(player.Actions.IsCasting);
        Assert.Equal(healthBefore, dummy.CurrentHealth);

        var events = world.Events.Drain();
        Assert.Contains(events, e => e is ActionEndedEvent ended
            && ended.Reason == Raid.Battle.Actions.ActionEndReason.CancelledByMove);
    }

    [Fact]
    public void Move_DoesNotCancelWindup()
    {
        var (world, player) = CreatePracticeWithPlayer(new WorldSettings
        {
            FixedDeltaMilliseconds = 500
        });

        var skill = player.FindSkill("test.meteor_strike")!;
        var dummy = world.Entities.Dummies().Single();
        var healthBefore = dummy.CurrentHealth;
        var startPosition = player.Position;
        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            PointTargetAt(dummy)));

        world.Loop.Tick();
        world.Loop.Tick();
        Assert.Equal(Raid.Battle.Actions.ActionPhaseKind.Windup, player.Actions.CurrentAction?.CurrentPhaseKind);

        world.Commands.Enqueue(new MoveCommand(player.Id, new Vector2(1f, 0f)));
        world.Loop.Tick();

        Assert.Equal(healthBefore, dummy.CurrentHealth);
        Assert.Equal(startPosition, player.Position);
        Assert.False(player.Actions.IsCasting);
    }

    [Fact]
    public void PositionSet_OverridesActiveMove()
    {
        var (world, player) = CreatePracticeWithPlayer();
        world.Commands.Enqueue(new MoveCommand(player.Id, new Vector2(10f, 0f)));
        world.Loop.Tick();

        world.Movement.SetPosition(new PositionSetRequest(
            player.Id,
            new Vector2(2f, 3f),
            PositionSetReason.Reset));

        Assert.Equal(new Vector2(2f, 3f), player.Position);
        Assert.Null(player.ActiveMovement);
    }

    [Fact]
    public void PracticeMode_LoadsMapFromJson()
    {
        var setup = BattleWorldFactory.Create(RaidMode.Practice, new WorldSettings());

        Assert.Equal("practice.default", setup.World.Map.Id);
        Assert.NotNull(setup.World.Map.Arena);
        Assert.Equal(Vector2.Zero, setup.World.Map.Arena.Value.Center);
        Assert.Equal(100f, setup.World.Map.Arena.Value.Radius);
        Assert.Empty(setup.World.Map.Obstacles ?? []);
        Assert.Equal(new Vector2(0f, 0f), setup.World.Map.PlayerSpawns![0].Position);
        Assert.Equal(new Vector2(5f, 0f), setup.World.Map.EntitySpawns![0].Position);
        Assert.Equal("practice.dummy", setup.World.Map.EntitySpawns[0].DefinitionId);
    }

    [Fact]
    public void PracticeMode_UsesTestClassSkills()
    {
        var setup = BattleWorldFactory.Create(RaidMode.Practice, new WorldSettings());
        var player = PracticeSpawn.Player(setup.World, "player-1", participantSlot: 0);

        Assert.Equal(RaidMode.Practice, setup.Mode);
        Assert.Equal("test", player.Class.ClassId);
        Assert.NotNull(player.FindSkill("test.instant_strike"));
        Assert.NotNull(player.FindSkill("test.meteor_strike"));
        Assert.NotNull(player.FindSkill("test.heal_pulse"));
        Assert.NotNull(player.FindSkill("test.grant_shield"));
        Assert.NotNull(player.FindSkill("test.bleed_strike"));
        Assert.NotNull(player.FindSkill("test.mana_tap"));
        Assert.NotNull(player.FindSkill("test.arrow_strike"));
        Assert.NotNull(player.FindSkill("test.hold_guard"));
        Assert.NotNull(player.FindSkill("test.charge_shot"));
        Assert.NotNull(player.FindSkill("test.direction_slash"));
        Assert.NotNull(player.FindSkill("test.basic_attack_b"));
        Assert.NotNull(player.FindSkill("test.basic_attack_a"));
        Assert.NotNull(player.FindSkill("test.zone_burn"));
        Assert.NotNull(player.FindSkill("test.zone_mend"));
        Assert.NotNull(player.FindSkill("test.stun_bolt"));
        Assert.NotNull(player.FindSkill("test.dash"));
        Assert.NotNull(player.FindSkill("test.chain_smash_a"));
        Assert.NotNull(player.FindSkill("test.chain_smash_b"));
        Assert.NotNull(player.FindSkill("test.chain_smash_c"));
        Assert.NotNull(player.FindSkill("test.stance_toggle"));
        Assert.NotNull(player.FindSkill("test.stance_shot_b"));
        Assert.NotNull(player.FindSkill("test.stance_shot_a"));
        Assert.True(player.FindSkill("test.basic_attack_b")!.IsBasicAttack);
        Assert.True(player.FindSkill("test.basic_attack_a")!.IsBasicAttack);
        Assert.Equal(
            new[]
            {
                "test.chain_smash_a",
                "test.stance_shot_b",
                "test.hold_guard",
                "test.meteor_strike",
                "test.stance_toggle",
                "test.dash"
            },
            player.SkillBar.SkillIds);
        Assert.Equal(SkillBarSlot.Q, player.SkillBar.GetSlot("test.chain_smash_a"));
        Assert.Equal(SkillBarSlot.F, player.SkillBar.GetSlot("test.dash"));
        Assert.Null(player.SkillBar.GetSlot("test.chain_smash_b"));
    }

    [Fact]
    public void PracticeMode_CanSelectBarSkills()
    {
        var setup = BattleWorldFactory.Create(RaidMode.Practice, new WorldSettings());
        var player = PracticeSpawn.Player(
            setup.World,
            "player-1",
            participantSlot: 0,
            barSkillIds:
            [
                "test.instant_strike",
                "test.arrow_strike"
            ]);

        Assert.Equal(
            new[] { "test.instant_strike", "test.arrow_strike" },
            player.SkillBar.SkillIds);
        Assert.Equal(SkillBarSlot.Q, player.SkillBar.GetSlot("test.instant_strike"));
        Assert.Equal(SkillBarSlot.W, player.SkillBar.GetSlot("test.arrow_strike"));
        Assert.Null(player.SkillBar.GetSlot("test.dash"));
    }

    [Fact]
    public void PracticeMode_CanSpawnMultiplePlayers()
    {
        var setup = BattleWorldFactory.Create(RaidMode.Practice, new WorldSettings());
        var first = PracticeSpawn.Player(setup.World, "player-1", participantSlot: 0);
        var second = PracticeSpawn.Player(setup.World, "player-2", participantSlot: 1);

        Assert.Equal(2, setup.World.Entities.Players().Count());
        Assert.NotEqual(first.Id, second.Id);
        Assert.NotEqual(first.Position, second.Position);
    }

    [Fact]
    public void EntitySkill_TurnsFacingTowardTarget()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.instant_strike")!;
        var dummy = world.Entities.Dummies().Single();
        var facingBefore = player.FacingDirection;

        var result = new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id))
            .Execute(new CommandContext(world));

        var expected = Vector2.Normalize(dummy.Position - player.Position);
        Assert.True(result.Succeeded);
        Assert.NotEqual(facingBefore, expected);
        Assert.Equal(facingBefore, player.FacingDirection);
        Assert.NotNull(player.ActiveMovement);
        Assert.True(Vector2.Distance(player.ActiveMovement!.DesiredFacingDirection, expected) < 0.001f);

        TickUntil(world, () => Vector2.Distance(player.FacingDirection, expected) < 0.02f);
        Assert.True(Vector2.Distance(player.FacingDirection, expected) < 0.02f);

        var events = world.Events.Drain();
        Assert.DoesNotContain(events, battleEvent => battleEvent is PositionSetEvent positionSet
            && positionSet.Reason == PositionSetReason.SkillFacing);
    }

    [Fact]
    public void DirectionSkill_TurnsFacingToSkillDirection()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.arrow_strike")!;
        var facingBefore = player.FacingDirection;

        var result = new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Direction, Direction: -Vector2.UnitX))
            .Execute(new CommandContext(world));

        Assert.True(result.Succeeded);
        Assert.Equal(facingBefore, player.FacingDirection);
        Assert.NotNull(player.ActiveMovement);
        Assert.True(Vector2.Distance(player.ActiveMovement!.DesiredFacingDirection, -Vector2.UnitX) < 0.001f);

        TickUntil(world, () => Vector2.Distance(player.FacingDirection, -Vector2.UnitX) < 0.02f);
        Assert.True(Vector2.Distance(player.FacingDirection, -Vector2.UnitX) < 0.02f);
    }

    [Fact]
    public void NoneSkill_DoesNotChangeFacing()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.hold_guard")!;
        var facingBefore = player.FacingDirection;

        var result = new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.None))
            .Execute(new CommandContext(world));

        Assert.True(result.Succeeded);
        Assert.Equal(facingBefore, player.FacingDirection);
    }

    private static void TickUntil(BattleWorld world, Func<bool> condition)
    {
        for (var i = 0; i < 80; i++)
        {
            if (condition())
            {
                return;
            }

            world.Loop.Tick();
        }

        Assert.Fail("Condition was not met.");
    }

    private static SkillTarget PointTargetAt(BattleEntity target) =>
        new(SkillTargetingMode.Point, Position: target.Position);

    private static (BattleWorld World, PlayerEntity Player) CreatePracticeWithPlayer(
        WorldSettings? settings = null)
    {
        var setup = BattleWorldFactory.Create(RaidMode.Practice, settings ?? new WorldSettings());
        var player = PracticeSpawn.Player(setup.World, "player-1", participantSlot: 0);
        return (setup.World, player);
    }
}
