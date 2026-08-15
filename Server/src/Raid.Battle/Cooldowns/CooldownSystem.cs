using Raid.Battle.Entities;
using Raid.Battle.Events;
using Raid.Battle.Snapshots;
using Raid.Battle.World;

namespace Raid.Battle.Cooldowns;

public sealed class CooldownSystem(BattleWorld world)
{
    public bool CanUseSkill(PlayerEntity player, string skillId)
    {
        if (world.Settings.Practice.IgnoreCooldowns)
        {
            return true;
        }

        return !player.SkillCooldownRemainingSeconds.TryGetValue(skillId, out var remaining)
            || remaining <= 0f;
    }

    public void StartCooldown(PlayerEntity player, string skillId, int cooldownMilliseconds)
    {
        if (cooldownMilliseconds <= 0 || world.Settings.Practice.IgnoreCooldowns)
        {
            return;
        }

        var durationSeconds = cooldownMilliseconds / 1000f;
        player.SkillCooldownRemainingSeconds[skillId] = durationSeconds;

        world.Events.Add(new CooldownStartedEvent(
            world.Tick,
            EntitySnapshot.FromEntity(player),
            skillId,
            durationSeconds));
    }

    public void Update(float deltaTime)
    {
        foreach (var player in world.Entities.Players())
        {
            if (player.SkillCooldownRemainingSeconds.Count == 0)
            {
                continue;
            }

            var readySkills = new List<string>();
            foreach (var (skillId, remaining) in player.SkillCooldownRemainingSeconds.ToArray())
            {
                var next = remaining - deltaTime;
                if (next <= 0f)
                {
                    player.SkillCooldownRemainingSeconds.Remove(skillId);
                    readySkills.Add(skillId);
                }
                else
                {
                    player.SkillCooldownRemainingSeconds[skillId] = next;
                }
            }

            foreach (var skillId in readySkills)
            {
                world.Events.Add(new CooldownReadyEvent(
                    world.Tick,
                    EntitySnapshot.FromEntity(player),
                    skillId));
            }
        }
    }
}
