using Raid.Battle.Actions;
using Raid.Battle.Commands;
using Raid.Battle.Effects;
using Raid.Battle.Entities;
using Raid.Battle.Events;
using Raid.Battle.World;
using Raid.Contracts.Common;

namespace Raid.Battle.Tests;

public sealed class CombatEffectTests
{
    [Fact]
    public void HealPulse_RestoresHealth_UpToMax()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.heal_pulse")!;
        player.CurrentHealth = 700f;

        UseSelfSkill(world, player, skill);
        TickUntil(world, () => player.CurrentHealth > 700f);

        Assert.Equal(900f, player.CurrentHealth);
        var events = world.Events.Drain();
        Assert.Contains(events, battleEvent => battleEvent is HealAppliedEvent heal
            && heal.Target.EntityId == player.Id
            && heal.Amount == 200f);
    }

    [Fact]
    public void HealPulse_DoesNotExceedMaxHealth()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.heal_pulse")!;
        player.CurrentHealth = 950f;

        UseSelfSkill(world, player, skill);
        TickUntil(world, () => player.CurrentHealth > 950f);

        Assert.Equal(player.MaxHealth, player.CurrentHealth);
    }

    [Fact]
    public void GrantShield_AbsorbsDamageBeforeHealth()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.grant_shield")!;
        var healthBefore = player.CurrentHealth;

        UseSelfSkill(world, player, skill);
        TickUntil(world, () => player.CurrentShield > 0f);

        Assert.Equal(150f, player.CurrentShield);

        world.Combat.ApplyDamage(
            player.Id,
            player.Id,
            80f,
            skill.SkillId,
            applyOnHitEffects: false);

        Assert.Equal(70f, player.CurrentShield);
        Assert.Equal(healthBefore, player.CurrentHealth);
    }

    [Fact]
    public void Shield_OverflowsIntoHealth()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.grant_shield")!;
        var healthBefore = player.CurrentHealth;

        UseSelfSkill(world, player, skill);
        TickUntil(world, () => player.CurrentShield > 0f);

        world.Combat.ApplyDamage(
            player.Id,
            player.Id,
            200f,
            skill.SkillId,
            applyOnHitEffects: false);

        Assert.Equal(0f, player.CurrentShield);
        Assert.Equal(healthBefore - 50f, player.CurrentHealth);
    }

    [Fact]
    public void HoldGuard_GrantsShieldAndImmunity_WhileHolding()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.hold_guard")!;

        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.None)));
        TickUntilPhase(world, player, ActionPhaseKind.Holding);

        Assert.Equal(200f, player.CurrentShield);
        Assert.True(world.Effects.Has(player.Id, StatusEffectKind.StatusImmunity));

        Assert.False(world.Effects.Apply(new StatusEffect(
            player.Id,
            StatusEffectKind.Bleed,
            player.Id,
            skill.SkillId,
            3f,
            1f,
            20f)));

        world.Commands.Enqueue(new ReleaseSkillCommand(player.Id, skill.SkillId));
        world.Loop.Tick();

        Assert.Equal(0f, player.CurrentShield);
        Assert.False(world.Effects.Has(player.Id, StatusEffectKind.StatusImmunity));
    }

    [Fact]
    public void BleedStrike_AppliesBleedAndTicksDamage()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.bleed_strike")!;
        var dummy = world.Entities.Dummies().Single();
        var healthBefore = dummy.CurrentHealth;

        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id)));
        TickUntil(world, () => dummy.CurrentHealth < healthBefore);

        Assert.Equal(healthBefore - skill.Damage, dummy.CurrentHealth);
        Assert.True(world.Effects.Has(dummy.Id, StatusEffectKind.Bleed));

        TickUntil(world, () => dummy.CurrentHealth < healthBefore - skill.Damage);

        Assert.Equal(healthBefore - skill.Damage - 20f, dummy.CurrentHealth);
    }

    [Fact]
    public void StatusImmunity_BlocksBleedApplication()
    {
        var (world, player) = CreatePracticeWithPlayer();
        var skill = player.FindSkill("test.bleed_strike")!;
        var dummy = world.Entities.Dummies().Single();
        var healthBefore = dummy.CurrentHealth;

        Assert.True(world.Effects.Apply(new StatusEffect(
            dummy.Id,
            StatusEffectKind.StatusImmunity,
            player.Id,
            "test.hold_guard",
            float.PositiveInfinity)));

        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id)));
        TickUntil(world, () => dummy.CurrentHealth < healthBefore);

        Assert.Equal(healthBefore - skill.Damage, dummy.CurrentHealth);
        Assert.False(world.Effects.Has(dummy.Id, StatusEffectKind.Bleed));
    }

    [Fact]
    public void ManaTap_RestoresPercentOfMaxManaOnHit()
    {
        var (world, player) = CreatePracticeWithPlayer(highManaRegen: false);
        var skill = player.FindSkill("test.mana_tap")!;
        var dummy = world.Entities.Dummies().Single();
        player.CurrentMana = skill.ManaCost;

        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id)));
        TickUntil(world, () => dummy.CurrentHealth < dummy.MaxHealth);

        Assert.Equal(100f, player.CurrentMana);
    }

    private static void UseSelfSkill(BattleWorld world, PlayerEntity player, Raid.Battle.Combat.SkillDefinition skill)
    {
        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.None)));
    }

    private static void TickUntilPhase(BattleWorld world, PlayerEntity player, ActionPhaseKind phase)
    {
        TickUntil(world, () => player.Actions.CurrentAction?.CurrentPhaseKind == phase);
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

    private static (BattleWorld World, PlayerEntity Player) CreatePracticeWithPlayer(bool highManaRegen = true)
    {
        var settings = new WorldSettings();
        settings.Practice.HighManaRegen = highManaRegen;
        var setup = BattleWorldFactory.Create(RaidMode.Practice, settings);
        var player = PracticeSpawn.Player(setup.World, "player-1", participantSlot: 0);
        return (setup.World, player);
    }
}
