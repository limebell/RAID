using System.Numerics;
using Raid.Battle.Commands;
using Raid.Battle.Entities;
using Raid.Contracts.Battle.Commands;
using Raid.Contracts.Common;

namespace Raid.GameServer.Sessions;

public static class SkillTargetMapper
{
    public static SkillTarget? ToBattleTarget(SkillTargetDto? dto)
    {
        if (dto is null)
        {
            return null;
        }

        return new SkillTarget(
            Mode: dto.Mode,
            EntityId: dto.EntityId is long entityId ? new EntityId(entityId) : null,
            Position: ToVector2(dto.Position),
            Direction: ToVector2(dto.Direction));
    }

    private static Vector2? ToVector2(Vector2Dto? value)
    {
        return value is null ? null : new Vector2(value.X, value.Y);
    }
}
