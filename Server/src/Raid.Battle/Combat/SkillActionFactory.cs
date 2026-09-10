using Raid.Battle.Actions;
using Raid.Battle.Actions.Phases;
using Raid.Battle.Commands;
using Raid.Battle.World;
using Raid.Contracts.Common;

namespace Raid.Battle.Combat;

public static class SkillActionFactory
{
    public static GameAction Create(
        ActionId actionId,
        Entities.EntityId ownerId,
        SkillDefinition skill,
        SkillTarget target,
        WorldSettings settings)
    {
        var phases = new List<IActionPhase>();

        if (skill.Kind == SkillKind.Cast && skill.CastTimeMilliseconds > 0)
        {
            phases.Add(new TimedPhase(
                ActionPhaseKind.Casting,
                settings.ToTicks(skill.CastTimeMilliseconds)));
        }

        if (skill.WindupTimeMilliseconds > 0)
        {
            phases.Add(new TimedPhase(
                ActionPhaseKind.Windup,
                settings.ToTicks(skill.WindupTimeMilliseconds)));
        }

        if (skill.Kind == SkillKind.Hold)
        {
            phases.Add(new HoldingPhase(
                ActionPhaseKind.Holding,
                skill.ChannelTimeMilliseconds / 1000f));
        }

        if (skill.Kind == SkillKind.Charge)
        {
            phases.Add(new HoldingPhase(
                ActionPhaseKind.Charging,
                skill.ChannelTimeMilliseconds / 1000f));
        }

        phases.Add(new ActivationPhase());

        if (skill.RecoveryTimeMilliseconds > 0)
        {
            phases.Add(new TimedPhase(
                ActionPhaseKind.Recovery,
                settings.ToTicks(skill.RecoveryTimeMilliseconds)));
        }

        return new GameAction(
            actionId,
            ownerId,
            skill,
            phases,
            target);
    }
}
