using Raid.Battle.World;

namespace Raid.Battle.Tests;

public sealed class PracticeSettingsTests
{
    [Fact]
    public void Apply_UpdatesOnlyProvidedFields()
    {
        var settings = new PracticeSettings
        {
            HighManaRegen = true,
            IgnoreCooldowns = false
        };

        settings.Apply(highManaRegen: false, ignoreCooldowns: true);

        Assert.False(settings.HighManaRegen);
        Assert.False(settings.IgnoreCooldowns);
    }
}
