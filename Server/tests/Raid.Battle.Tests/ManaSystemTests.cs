using System.Numerics;
using Raid.Battle.Actions;
using Raid.Battle.Commands;
using Raid.Battle.Definitions;
using Raid.Battle.Entities;
using Raid.Battle.Events;
using Raid.Battle.World;
using Raid.Contracts.Common;

namespace Raid.Battle.Tests;

public sealed class ManaSystemTests
{
    [Fact]
    public void Player_StartsWithFullMana()
    {
        var (world, player) = CreatePracticeWithPlayer(finiteMana: true);

        Assert.Equal(TestClassDefinition.MaxMana, player.CurrentMana);
        Assert.Equal(TestClassDefinition.MaxMana, player.MaxMana);
        Assert.Equal(TestClassDefinition.ManaRegenPerSecond, player.ManaRegenPerSecond);
    }

    [Fact]
    public void Activation_SpendsMana_AndAppliesDamage()
    {
        var (world, player) = CreatePracticeWithPlayer(finiteMana: true);
        var skill = TestClassDefinition.InstantStrike;
        var dummy = world.Entities.Dummies().Single();

        player.CurrentMana = skill.ManaCost - TestClassDefinition.ManaRegenPerSecond;
        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id)));
        world.Loop.Tick();

        Assert.Equal(0f, player.CurrentMana);
        Assert.Equal(100_000f - skill.Damage, dummy.CurrentHealth);
    }

    [Fact]
    public void Activation_SkipsDamage_WhenManaIsInsufficient()
    {
        var (world, player) = CreatePracticeWithPlayer(finiteMana: true);
        var skill = TestClassDefinition.InstantStrike;
        var dummy = world.Entities.Dummies().Single();
        var healthBefore = dummy.CurrentHealth;

        player.CurrentMana = skill.ManaCost - TestClassDefinition.ManaRegenPerSecond - 1f;
        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id)));
        world.Loop.Tick();

        Assert.Equal(skill.ManaCost - 1f, player.CurrentMana);
        Assert.Equal(healthBefore, dummy.CurrentHealth);
    }

    [Fact]
    public void Casting_AllowsManaRegen()
    {
        var settings = new WorldSettings
        {
            FixedDeltaMilliseconds = 500
        };
        settings.Practice.HighManaRegen = false;
        var setup = BattleWorldFactory.Create(RaidMode.Practice, settings);
        var world = setup.World;
        var player = BattleWorldFactory.SpawnPlayer(world, "player-1", participantSlot: 0);
        var skill = TestClassDefinition.ChargedStrike;
        var dummy = world.Entities.Dummies().Single();

        player.CurrentMana = 0f;
        world.Commands.Enqueue(new UseSkillCommand(
            player.Id,
            skill,
            new SkillTarget(SkillTargetingMode.Entity, EntityId: dummy.Id)));

        world.Loop.Tick();
        Assert.True(player.Actions.IsCasting);
        Assert.Equal(0f, player.CurrentMana);

        world.Loop.Tick();
        Assert.True(player.Actions.IsBusy);
        Assert.Equal(TestClassDefinition.ManaRegenPerSecond, player.CurrentMana);
    }

    [Fact]
    public void Downed_StopsManaRegen()
    {
        var (world, player) = CreatePracticeWithPlayer(finiteMana: true);

        player.CurrentMana = 0f;
        player.IsDowned = true;

        for (var i = 0; i < 20; i++)
        {
            world.Loop.Tick();
        }

        Assert.Equal(0f, player.CurrentMana);
    }

    [Fact]
    public void PracticeHighManaRegen_RefillsManaQuickly()
    {
        var (world, player) = CreatePracticeWithPlayer(finiteMana: false);

        player.CurrentMana = 0f;
        world.Loop.Tick();

        Assert.Equal(TestClassDefinition.MaxMana, player.CurrentMana);
    }

    [Fact]
    public void PracticeHighManaRegen_CanBeDisabledMidSession()
    {
        var (world, player) = CreatePracticeWithPlayer(finiteMana: false);

        world.Settings.Practice.HighManaRegen = false;
        player.CurrentMana = 0f;

        world.Loop.Tick();

        Assert.Equal(TestClassDefinition.ManaRegenPerSecond, player.CurrentMana);
    }

    [Fact]
    public void NaturalRegen_EmitsResourceChangedOnPulse()
    {
        var (world, player) = CreatePracticeWithPlayer(finiteMana: true);

        player.CurrentMana = 0f;
        world.Loop.Tick();

        var events = world.Events.Drain();
        Assert.Contains(events, battleEvent => battleEvent is ResourceChangedEvent changed
            && changed.Entity.EntityId == player.Id
            && changed.Entity.CurrentMana == TestClassDefinition.ManaRegenPerSecond);
    }

    private static (BattleWorld World, PlayerEntity Player) CreatePracticeWithPlayer(bool finiteMana)
    {
        var settings = new WorldSettings
        {
            FixedDeltaMilliseconds = 1000
        };
        settings.Practice.HighManaRegen = !finiteMana;
        var setup = BattleWorldFactory.Create(RaidMode.Practice, settings);
        var player = BattleWorldFactory.SpawnPlayer(setup.World, "player-1", participantSlot: 0);
        return (setup.World, player);
    }
}
