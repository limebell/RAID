using System.Text.Json;
using System.Text.Json.Serialization;

namespace Raid.Battle.Definitions;

public static class EmbeddedJsonLoader
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new Vector2JsonConverter(),
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    public static T Load<T>(string resourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourcePath);

        var resourceName = $"Raid.Battle.{resourcePath}";
        using var stream = typeof(EmbeddedJsonLoader).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");

        return JsonSerializer.Deserialize<T>(stream, Options)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' could not be read.");
    }
}
