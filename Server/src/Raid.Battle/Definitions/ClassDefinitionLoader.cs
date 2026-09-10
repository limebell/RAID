using System.Collections.Concurrent;
using Raid.Battle.Combat;

namespace Raid.Battle.Definitions;

public static class ClassDefinitionLoader
{
    private static readonly ConcurrentDictionary<string, PlayerClassDefinition> Cache = new(StringComparer.Ordinal);

    public static PlayerClassDefinition Load(string classId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(classId);
        return Cache.GetOrAdd(classId, id =>
            EmbeddedJsonLoader.Load<PlayerClassDefinition>($"Data.Classes.{id}.json"));
    }
}
