namespace Raid.Battle.World;

public sealed class PracticeSettings
{
    public bool HighManaRegen { get; set; } = true;

    public bool IgnoreCooldowns { get; set; }

    public void Apply(bool highManaRegen, bool ignoreCooldowns)
    {
        HighManaRegen = highManaRegen;
        IgnoreCooldowns = ignoreCooldowns;
    }
}
