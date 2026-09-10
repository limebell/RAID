using Raid.Battle.Snapshots;
using Raid.Contracts.Common;

namespace Raid.Battle.Events;

public sealed record StatusEffectRemovedEvent(
    long Tick,
    EntitySnapshot Target,
    string? SkillId,
    StatusEffectKind Kind,
    string? BuffId = null) : IBattleEvent;
