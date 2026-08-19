using Raid.Contracts.Battle.Snapshots;
using Raid.Contracts.Common;
using Raid.Contracts.Session;

namespace Raid.Contracts.Battle;

public sealed record JoinSessionResponse(
    Guid SessionId,
    RaidMode Mode,
    long PlayerEntityId,
    int ParticipantSlot,
    string ClassId,
    IReadOnlyList<SkillInfoDto> Skills,
    PracticeSettingsDto PracticeSettings,
    BattleSnapshotDto Snapshot);
