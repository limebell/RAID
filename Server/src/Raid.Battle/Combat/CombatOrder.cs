using System.Numerics;
using Raid.Battle.Commands;
using Raid.Battle.Entities;

namespace Raid.Battle.Combat;

public enum CombatOrderKind
{
    None = 0,
    AttackTarget = 1,
    AttackMove = 2,
    ChaseCast = 3
}

internal sealed class CombatOrder
{
    public CombatOrderKind Kind { get; private set; }

    public EntityId? TargetId { get; private set; }

    public Vector2? Destination { get; private set; }

    public SkillDefinition? Skill { get; private set; }

    public SkillTarget? SkillTarget { get; private set; }

    public bool IsActive => Kind != CombatOrderKind.None;

    public void Clear()
    {
        Kind = CombatOrderKind.None;
        TargetId = null;
        Destination = null;
        Skill = null;
        SkillTarget = null;
    }

    public void SetAttackTarget(EntityId targetId)
    {
        Kind = CombatOrderKind.AttackTarget;
        TargetId = targetId;
        Destination = null;
        Skill = null;
        SkillTarget = null;
    }

    public void SetAttackMove(Vector2 destination)
    {
        Kind = CombatOrderKind.AttackMove;
        TargetId = null;
        Destination = destination;
        Skill = null;
        SkillTarget = null;
    }

    public void SetChaseCast(SkillDefinition skill, SkillTarget target)
    {
        Kind = CombatOrderKind.ChaseCast;
        TargetId = target.EntityId;
        Destination = null;
        Skill = skill;
        SkillTarget = target;
    }
}
