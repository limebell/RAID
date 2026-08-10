namespace Raid.Contracts.Battle.Commands;

public sealed record UseSkillRequest(
    int ClientSequence,
    string SkillId,
    SkillTargetDto? Target = null);
