using System.Collections.Concurrent;

namespace Raid.Battle.Definitions;

public static class EntityDefinitionLoader
{
    private static readonly ConcurrentDictionary<string, EntityDefinition> Cache = new(StringComparer.Ordinal);

    public static EntityDefinition Load(string definitionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(definitionId);
        return Cache.GetOrAdd(definitionId, id => EmbeddedJsonLoader.Load<EntityDefinition>($"Data.Entities.{id}.json"));
    }
}
