using Raid.Battle.Actions;
using Raid.Battle.Actions.Phases;
using Raid.Battle.Commands;
using Raid.Battle.World;

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

        if (!skill.IsInstant)
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
            skill.SkillId,
            phases,
            target.EntityId,
            skill.Damage);
    }
}
