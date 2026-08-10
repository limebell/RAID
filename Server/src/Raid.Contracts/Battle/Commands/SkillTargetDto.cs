using Raid.Contracts.Common;

namespace Raid.Contracts.Battle.Commands;

public sealed record SkillTargetDto(
    SkillTargetingMode Mode,
    long? EntityId = null,
    Vector2Dto? Position = null,
    Vector2Dto? Direction = null);
