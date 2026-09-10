using System.Numerics;
using Raid.Battle.Combat;
using Raid.Battle.Commands;
using Raid.Battle.Entities;
using Raid.Battle.Movement;
using Raid.Battle.World;
using Raid.Contracts.Common;

namespace Raid.Battle.Tests;

public sealed class ChainStanceTests
{
    [Fact]
    public void ChainSmash_OpenerRepeatsIntoFollowUps_ThenStartsRootCooldown()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var opener = player.FindSkill("test.chain_smash_a")!;
        var dummy = world.Entities.Dummies().Single();
        var health = dummy.CurrentHealth;

        UseChain(world, player, opener);
        TickUntil(world, () => dummy.CurrentHealth < health);
        Assert.Equal(health - 50f, dummy.CurrentHealth);
        Assert.True(world.Cooldowns.CanUseSkill(player, opener.SkillId));

        health = dummy.CurrentHealth;
        TickUntil(world, () => !player.Actions.IsBusy);
        UseChain(world, player, opener);
        TickUntil(world, () => dummy.CurrentHealth < health);
        Assert.Equal(health - 75f, dummy.CurrentHealth);
        Assert.True(world.Cooldowns.CanUseSkill(player, opener.SkillId));

        health = dummy.CurrentHealth;
        TickUntil(world, () => !player.Actions.IsBusy);
        UseChain(world, player, opener);
        TickUntil(world, () => dummy.CurrentHealth < health);
        Assert.Equal(health - 100f, dummy.CurrentHealth);
        Assert.False(world.Cooldowns.CanUseSkill(player, opener.SkillId));
    }

    [Fact]
    public void ChainSmash_CanUseFollowUpSkillIdDuringWindow()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var opener = player.FindSkill("test.chain_smash_a")!;
        var followUp = player.FindSkill("test.chain_smash_b")!;
        var dummy = world.Entities.Dummies().Single();
        var health = dummy.CurrentHealth;

        UseChain(world, player, opener);
        TickUntil(world, () => dummy.CurrentHealth < health);
        TickUntil(world, () => !player.Actions.IsBusy);

        health = dummy.CurrentHealth;
        UseChain(world, player, followUp);
        TickUntil(world, () => dummy.CurrentHealth < health);
        Assert.Equal(health - 75f, dummy.CurrentHealth);
        Assert.True(world.Cooldowns.CanUseSkill(player, opener.SkillId));
    }

    [Fact]
    public void ChainSmash_FollowUpWithoutWindow_Fails()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var followUp = player.FindSkill("test.chain_smash_b")!;
        var dummy = world.Entities.Dummies().Single();

        var result = new UseSkillCommand(
            player.Id,
            followUp,
            new SkillTarget(SkillTargetingMode.Direction, Direction: Vector2.UnitX))
            .Execute(new CommandContext(world));

        Assert.False(result.Succeeded);
        Assert.Equal(UseSkillFailureReason.ChainNotAvailable, result.UseSkillFailure);
        Assert.False(player.Actions.IsBusy);
    }

    [Fact]
    public void ChainSmash_Miss_StartsCooldown()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var opener = player.FindSkill("test.chain_smash_a")!;
        var dummy = world.Entities.Dummies().Single();
        var healthBefore = dummy.CurrentHealth;
        world.Movement.SetPosition(new PositionSetRequest(
            dummy.Id,
            new Vector2(3f, 5f),
            PositionSetReason.Reset));

        UseChain(world, player, opener);
        TickUntilIdle(world, player);

        Assert.Equal(healthBefore, dummy.CurrentHealth);
        Assert.False(world.Cooldowns.CanUseSkill(player, opener.SkillId));
    }

    [Fact]
    public void ChainSmash_WindowExpire_StartsCooldown()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var opener = player.FindSkill("test.chain_smash_a")!;
        var dummy = world.Entities.Dummies().Single();
        var healthBefore = dummy.CurrentHealth;

        UseChain(world, player, opener);
        TickUntil(world, () => dummy.CurrentHealth < healthBefore);
        Assert.True(world.Cooldowns.CanUseSkill(player, opener.SkillId));

        for (var i = 0; i < 20; i++)
        {
            world.Loop.Tick();
        }

        Assert.False(world.Cooldowns.CanUseSkill(player, opener.SkillId));
    }

    [Fact]
    public void StanceToggle_SwapsNamedBuffs_AndSelectsMatchingSkills()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var toggle = player.FindSkill("test.stance_toggle")!;
        var basicB = player.FindSkill("test.basic_attack_b")!;
        var basicA = player.FindSkill("test.basic_attack_a")!;

        Assert.True(world.Effects.HasBuff(player.Id, "test.stance_b"));
        Assert.Equal(basicB.SkillId, SkillResolver.FindBasicAttack(world, player)!.SkillId);
        Assert.Equal(basicB.SkillId, SkillResolver.Resolve(world, player, basicA).SkillId);

        ToggleStance(world, player, toggle);
        Assert.True(world.Effects.HasBuff(player.Id, "test.stance_a"));
        Assert.Equal(basicA.SkillId, SkillResolver.FindBasicAttack(world, player)!.SkillId);
        Assert.Equal(basicA.SkillId, SkillResolver.Resolve(world, player, basicB).SkillId);

        for (var i = 0; i < 20; i++)
        {
            world.Loop.Tick();
        }

        Assert.True(world.Effects.HasBuff(player.Id, "test.stance_a"));

        ToggleStance(world, player, toggle);
        Assert.True(world.Effects.HasBuff(player.Id, "test.stance_b"));
        Assert.Equal(basicB.SkillId, SkillResolver.FindBasicAttack(world, player)!.SkillId);
    }

    [Fact]
    public void StanceToggle_ChangesBasicAttackDamage()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var toggle = player.FindSkill("test.stance_toggle")!;
        var basicB = player.FindSkill("test.basic_attack_b")!;
        var dummy = world.Entities.Dummies().Single();
        var healthBefore = dummy.CurrentHealth;

        UseBasic(world, player, basicB, dummy);
        TickUntil(world, () => dummy.CurrentHealth < healthBefore);
        Assert.Equal(healthBefore - 40f, dummy.CurrentHealth);
        player.CombatOrder.Clear();

        TickUntil(world, () => !player.Actions.IsBusy
            && world.Cooldowns.CanUseSkill(player, basicB.SkillId));
        ToggleStance(world, player, toggle);

        healthBefore = dummy.CurrentHealth;
        UseBasic(world, player, basicB, dummy);
        TickUntil(world, () => dummy.CurrentHealth < healthBefore);
        Assert.Equal(healthBefore - 70f, dummy.CurrentHealth);
        player.CombatOrder.Clear();

        TickUntil(world, () => !player.Actions.IsBusy
            && world.Cooldowns.CanUseSkill(player, toggle.SkillId));
        ToggleStance(world, player, toggle);
        Assert.True(world.Effects.HasBuff(player.Id, "test.stance_b"));
    }

    [Fact]
    public void StanceShot_GainsRangeOnStanceA()
    {
        var (world, player) = CreatePracticeWithPlayer();
        world.Settings.Practice.IgnoreCooldowns = true;
        var toggle = player.FindSkill("test.stance_toggle")!;
        var shotB = player.FindSkill("test.stance_shot_b")!;
        var dummy = world.Entities.Dummies().Single();
        world.Movement.SetPosition(new PositionSetRequest(
            dummy.Id,
            new Vector2(10f, 0f),
            PositionSetReason.Reset));
        var healthBefore = dummy.CurrentHealth;

        UseShot(world, player, shotB);
        TickUntilIdle(world, player);
        Assert.Equal(healthBefore, dummy.CurrentHealth);

        ToggleStance(world, player, toggle);
        Assert.True(world.Effects.HasBuff(player.Id, "test.stance_a"));

        UseShot(world, player, shotB);
        TickUntil(world, () => dummy.CurrentHealth < healthBefore);
        Assert.Equal(healthBefore - 80f, dummy.CurrentHealth);
    }

    private static void ToggleStance(
        BattleWorld world,
        PlayerEntity player,
        SkillDefinition toggle)
    {
        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            toggle,
            new SkillTarget(SkillTargetingMode.None)));
        world.Loop.Tick();
    }

    private static void UseChain(BattleWorld world, PlayerEntity player, SkillDefinition skill)
    {
        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Direction, Direction: Vector2.UnitX)));
    }

    private static void UseShot(BattleWorld world, PlayerEntity player, Raid.Battle.Combat.SkillDefinition skill)
    {
        UseChain(world, player, skill);
    }

    private static void UseBasic(
        BattleWorld world,
        PlayerEntity player,
        Raid.Battle.Combat.SkillDefinition skill,
        DummyEntity dummy)
    {
        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id)));
    }

    private static void TickUntilIdle(BattleWorld world, PlayerEntity player)
    {
        TickUntil(world, () => player.Actions.IsBusy);
        TickUntil(world, () => !player.Actions.IsBusy);
    }

    private static void TickUntil(BattleWorld world, Func<bool> condition)
    {
        for (var i = 0; i < 200; i++)
        {
            if (condition())
            {
                return;
            }

            world.Loop.Tick();
        }

        Assert.Fail("Condition was not met.");
    }

    private static (BattleWorld World, PlayerEntity Player) CreatePracticeWithPlayer()
    {
        var setup = BattleWorldFactory.Create(RaidMode.Practice, new WorldSettings());
        var player = PracticeSpawn.Player(setup.World, "player-1", participantSlot: 0);
        return (setup.World, player);
    }
}
