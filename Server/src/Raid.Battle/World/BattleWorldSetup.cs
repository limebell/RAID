using Raid.Contracts.Common;

namespace Raid.Battle.World;

public sealed record BattleWorldSetup(
    RaidMode Mode,
    BattleWorld World);
