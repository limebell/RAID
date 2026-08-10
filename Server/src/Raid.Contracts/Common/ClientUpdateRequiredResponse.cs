namespace Raid.Contracts.Common;

public sealed record ClientUpdateRequiredResponse(
    string RequiredVersion,
    string CurrentVersion);
