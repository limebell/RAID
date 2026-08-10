namespace Raid.Contracts.Battle.Commands;

public sealed record MoveRequest(
    int ClientSequence,
    float X,
    float Y);
