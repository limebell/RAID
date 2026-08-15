namespace Raid.Battle.World;

public sealed class WorldSettings
{
    public const int DefaultFixedDeltaMilliseconds = 50;

    public const float PracticeHighManaRegenPerSecond = 100_000f;

    public int FixedDeltaMilliseconds { get; init; } = DefaultFixedDeltaMilliseconds;

    public PracticeSettings Practice { get; } = new();

    public float FixedDeltaTimeSeconds => FixedDeltaMilliseconds / 1000f;

    public int ToTicks(int durationMilliseconds)
    {
        if (durationMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationMilliseconds));
        }

        if (durationMilliseconds == 0)
        {
            return 0;
        }

        return (durationMilliseconds + FixedDeltaMilliseconds - 1) / FixedDeltaMilliseconds;
    }
}
