namespace Raid.Contracts.Session;

public sealed record UpdatePracticeSettingsRequest(
    bool HighManaRegen,
    bool IgnoreCooldowns);
