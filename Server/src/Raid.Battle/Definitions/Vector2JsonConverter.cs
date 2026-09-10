using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Raid.Battle.Definitions;

public sealed class Vector2JsonConverter : JsonConverter<Vector2>
{
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Vector2 must be a JSON object.");
        }

        float x = 0f;
        float y = 0f;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return new Vector2(x, y);
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Vector2 object is invalid.");
            }

            var propertyName = reader.GetString();
            reader.Read();

            if (propertyName is "x" or "X")
            {
                x = reader.GetSingle();
            }
            else if (propertyName is "y" or "Y")
            {
                y = reader.GetSingle();
            }
            else
            {
                reader.Skip();
            }
        }

        throw new JsonException("Vector2 object is incomplete.");
    }

    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteEndObject();
    }
}
