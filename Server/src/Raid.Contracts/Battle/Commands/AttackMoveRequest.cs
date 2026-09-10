namespace Raid.Contracts.Battle.Commands;

public sealed record AttackMoveRequest(
    int ClientSequence,
    float X,
    float Y);
