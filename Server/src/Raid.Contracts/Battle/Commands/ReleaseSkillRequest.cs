namespace Raid.Contracts.Battle.Commands;

public sealed record ReleaseSkillRequest(
    int ClientSequence,
    string SkillId);
