using Raid.Battle.Commands;
using Raid.Battle.Entities;
using Raid.Battle.Events;
using Raid.Battle.World;
using Raid.Contracts.Common;

namespace Raid.Battle.Tests;

public sealed class BasicAttackTests
{
    [Fact]
    public void BasicAttack_IsEntityTargetedInstantWithNoManaCost()
    {
        var (_, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.basic_attack_b")!;

        Assert.True(skill.IsBasicAttack);
        Assert.Equal(SkillKind.Instant, skill.Kind);
        Assert.Equal(SkillTargetingMode.Entity, skill.TargetingMode);
        Assert.Equal(0, skill.ManaCost);
        Assert.True(skill.CooldownMilliseconds > 0);
        Assert.True(skill.CooldownMilliseconds < 3000);
    }

    [Fact]
    public void BasicAttack_DamagesDummy_WithoutSpendingMana()
    {
        var (world, player, skill, dummy, healthBefore) = CreateScenario();
        var manaBefore = player.CurrentMana;

        TickUntilRecovering(world, player, skill, dummy);

        Assert.Equal(manaBefore, player.CurrentMana);
        Assert.Equal(healthBefore - skill.Damage, dummy.CurrentHealth);
        Assert.Contains(world.Events.Drain(), battleEvent => battleEvent is DamageAppliedEvent damage
            && damage.Target.EntityId == dummy.Id
            && damage.Amount == skill.Damage
            && damage.SkillId == skill.SkillId);
    }

    [Fact]
    public void BasicAttack_StartsCooldown()
    {
        var (world, player, skill, dummy, _) = CreateScenario();
        world.Settings.Practice.IgnoreCooldowns = false;

        TickUntilRecovering(world, player, skill, dummy);

        Assert.False(world.Cooldowns.CanUseSkill(player, skill.SkillId));
    }

    private static void TickUntilRecovering(
        BattleWorld world,
        PlayerEntity player,
        Raid.Battle.Combat.SkillDefinition skill,
        BattleEntity dummy)
    {
        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id)));

        for (var i = 0; i < 40; i++)
        {
            world.Loop.Tick();
            if (player.Actions.IsRecovering)
            {
                return;
            }
        }

        Assert.Fail("Did not reach recovery after basic attack activation.");
    }

    private static (BattleWorld World, PlayerEntity Player, Raid.Battle.Combat.SkillDefinition Skill, DummyEntity Dummy, float HealthBefore)
        CreateScenario()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.basic_attack_b")!;
        var dummy = world.Entities.Dummies().Single();
        return (world, player, skill, dummy, dummy.CurrentHealth);
    }

    private static (BattleWorld World, PlayerEntity Player) CreatePracticeWithPlayer()
    {
        var setup = BattleWorldFactory.Create(RaidMode.Practice, new WorldSettings
        {
            FixedDeltaMilliseconds = 50
        });
        var player = PracticeSpawn.Player(setup.World, "player-1", participantSlot: 0);
        return (setup.World, player);
    }
}
