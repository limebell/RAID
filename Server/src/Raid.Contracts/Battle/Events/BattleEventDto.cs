using Raid.Contracts.Common;

namespace Raid.Contracts.Battle.Events;

public sealed record BattleEventDto(
    BattleEventType Type,
    long Tick,
    long? EntityId = null,
    long? TargetEntityId = null,
    long? OwnerId = null,
    long? AttackerId = null,
    Vector2Dto? Position = null,
    Vector2Dto? FacingDirection = null,
    string? SkillId = null,
    string? Phase = null,
    string? Reason = null,
    float? Amount = null,
    float? RemainingHealth = null);
