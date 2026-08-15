using Raid.Contracts.Common;

namespace Raid.Contracts.Session;

/// <summary>
/// Session-level event envelope. Optional fields depend on <see cref="Type"/>.
/// </summary>
/// <param name="Type">Event kind. Determines which optional fields are populated.</param>
/// <param name="PracticeSettings">
/// Updated practice settings. Used by <see cref="SessionEventType.PracticeSettingsChanged"/> only.
/// </param>
public sealed record SessionEventDto(
    SessionEventType Type,
    PracticeSettingsDto? PracticeSettings = null);
