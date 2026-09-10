using System.Collections.Concurrent;

namespace Raid.Battle.Definitions;

public static class MapDefinitionLoader
{
    private static readonly ConcurrentDictionary<string, MapDefinition> Cache = new(StringComparer.Ordinal);

    public static MapDefinition Load(string mapId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapId);
        return Cache.GetOrAdd(mapId, id =>
        {
            var map = EmbeddedJsonLoader.Load<MapDefinition>($"Data.Maps.{id}.json");
            return map with
            {
                Obstacles = map.Obstacles ?? []
            };
        });
    }
}
