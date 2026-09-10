using System.Collections.Concurrent;
using Raid.Contracts.Common;

namespace Raid.Battle.Definitions;

public static class ModeDefinitionLoader
{
    private static readonly ConcurrentDictionary<string, ModeDefinition> Cache = new(StringComparer.Ordinal);

    public static ModeDefinition Load(RaidMode mode)
    {
        var modeId = mode switch
        {
            RaidMode.Practice => "practice",
            RaidMode.Raid => "raid",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

        return Load(modeId);
    }

    public static ModeDefinition Load(string modeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modeId);
        return Cache.GetOrAdd(modeId, id => EmbeddedJsonLoader.Load<ModeDefinition>($"Data.Modes.{id}.json"));
    }
}
