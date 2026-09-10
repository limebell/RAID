using Raid.Battle.Entities;
using Raid.Battle.World;

namespace Raid.Battle.Combat;

public sealed class SkillChainSystem(BattleWorld world)
{
    public SkillDefinition ResolveFollowUp(PlayerEntity player, SkillDefinition requested)
    {
        var chain = player.ChainState;
        if (chain?.NextSkillId is null || chain.WindowRemainingSeconds <= 0f)
        {
            return requested;
        }

        if (!string.Equals(requested.SkillId, chain.RootSkillId, StringComparison.Ordinal)
            && !string.Equals(requested.SkillId, chain.NextSkillId, StringComparison.Ordinal))
        {
            return requested;
        }

        return player.FindSkill(chain.NextSkillId) ?? requested;
    }

    public bool IsUnavailableFollowUp(PlayerEntity player, SkillDefinition skill)
    {
        if (!player.Class.IsChainFollowUp(skill.SkillId))
        {
            return false;
        }

        var chain = player.ChainState;
        return chain?.NextSkillId is null
            || chain.WindowRemainingSeconds <= 0f
            || !string.Equals(chain.NextSkillId, skill.SkillId, StringComparison.Ordinal);
    }

    public bool DefersCooldown(PlayerEntity player, SkillDefinition skill)
    {
        if (skill.CanChain)
        {
            return true;
        }

        var chain = player.ChainState;
        return chain is not null
            && (string.Equals(chain.NextSkillId, skill.SkillId, StringComparison.Ordinal)
                || string.Equals(chain.CurrentSkillId, skill.SkillId, StringComparison.Ordinal));
    }

    public void OnSkillStarting(PlayerEntity player, SkillDefinition skill)
    {
        var chain = player.ChainState;
        if (chain is null)
        {
            return;
        }

        if (string.Equals(chain.NextSkillId, skill.SkillId, StringComparison.Ordinal))
        {
            return;
        }

        Finish(player, chain);
    }

    public void Begin(PlayerEntity player, SkillDefinition skill)
    {
        if (!skill.CanChain && !IsExpectedFollowUp(player, skill))
        {
            return;
        }

        var chain = player.ChainState;
        if (chain is not null && IsExpectedFollowUp(player, skill))
        {
            chain.CurrentSkillId = skill.SkillId;
            chain.NextSkillId = null;
            chain.PendingResolution = true;
            chain.HitConnected = false;
            return;
        }

        chain = new SkillChainState(skill.SkillId, skill.SkillId)
        {
            PendingResolution = true,
            HitConnected = false
        };
        player.ChainState = chain;
    }

    public void RegisterHit(EntityId attackerId, string skillId)
    {
        if (world.Entities.Find(attackerId) is not PlayerEntity player)
        {
            return;
        }

        var chain = player.ChainState;
        if (chain is null
            || !chain.PendingResolution
            || !string.Equals(chain.CurrentSkillId, skillId, StringComparison.Ordinal))
        {
            return;
        }

        chain.HitConnected = true;
    }

    public void ResolvePending()
    {
        foreach (var player in world.Entities.Players())
        {
            var chain = player.ChainState;
            if (chain is null || !chain.PendingResolution)
            {
                continue;
            }

            chain.PendingResolution = false;
            var skill = player.FindSkill(chain.CurrentSkillId);
            if (skill is null)
            {
                player.ChainState = null;
                continue;
            }

            if (chain.HitConnected && skill.CanChain && skill.ChainWindowMilliseconds > 0)
            {
                chain.NextSkillId = skill.ChainToSkillId;
                chain.WindowRemainingSeconds = skill.ChainWindowMilliseconds / 1000f;
                continue;
            }

            Finish(player, chain);
        }
    }

    public void Update(float deltaTime)
    {
        foreach (var player in world.Entities.Players())
        {
            var chain = player.ChainState;
            if (chain is null || chain.PendingResolution)
            {
                continue;
            }

            if (IsUsingChainSkill(player, chain))
            {
                continue;
            }

            if (chain.WindowRemainingSeconds <= 0f)
            {
                Finish(player, chain);
                continue;
            }

            chain.WindowRemainingSeconds -= deltaTime;
            if (chain.WindowRemainingSeconds > 0f)
            {
                continue;
            }

            Finish(player, chain);
        }
    }

    private static bool IsUsingChainSkill(PlayerEntity player, SkillChainState chain)
    {
        var skillId = player.Actions.CurrentAction?.SkillId;
        if (skillId is null)
        {
            return false;
        }

        return string.Equals(chain.NextSkillId, skillId, StringComparison.Ordinal);
    }

    private static bool IsExpectedFollowUp(PlayerEntity player, SkillDefinition skill)
    {
        var chain = player.ChainState;
        return chain?.NextSkillId is not null
            && chain.WindowRemainingSeconds > 0f
            && string.Equals(chain.NextSkillId, skill.SkillId, StringComparison.Ordinal);
    }

    private void Finish(PlayerEntity player, SkillChainState chain)
    {
        var root = player.FindSkill(chain.RootSkillId);
        if (root is not null)
        {
            world.Cooldowns.StartCooldown(player, root.SkillId, root.CooldownMilliseconds);
        }

        player.ChainState = null;
    }
}
