using Raid.Battle.Entities;
using Raid.Battle.World;

namespace Raid.Battle.Actions;

public sealed class ActionContext(BattleWorld world, BattleEntity owner, GameAction action)
{
    public BattleWorld World { get; } = world;

    public BattleEntity Owner { get; } = owner;

    public GameAction Action { get; } = action;

    public EntityId? TargetId { get; init; }

    public float Damage { get; init; }
}
