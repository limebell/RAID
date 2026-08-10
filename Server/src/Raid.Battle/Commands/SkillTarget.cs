using System.Numerics;
using Raid.Battle.Entities;
using Raid.Contracts.Common;

namespace Raid.Battle.Commands;

public sealed record SkillTarget(
    SkillTargetingMode Mode,
    EntityId? EntityId = null,
    Vector2? Position = null,
    Vector2? Direction = null);
