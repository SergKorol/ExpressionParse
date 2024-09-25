using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExpressionParse;

public class DictionaryStringObjectJsonConverter : JsonConverter<IReadOnlyDictionary<string, object>>
{
    public override IReadOnlyDictionary<string, object>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dictionary = new Dictionary<string, object>();
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return dictionary.ToFrozenDictionary();
            }

            string key = reader.GetString()!;

            reader.Read();
            object value;

            if (reader.TokenType == JsonTokenType.String)
            {
                value = reader.GetString()!;
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                value = reader.GetInt32();
            }
            else if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
            {
                value = reader.GetBoolean();
            }
            else
            {
                value = JsonDocument.ParseValue(ref reader).RootElement.Clone();
            }

            dictionary[key] = value;
        }

        return dictionary.ToFrozenDictionary();
    }

    public override void Write(Utf8JsonWriter writer, IReadOnlyDictionary<string, object> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var kvp in value)
        {
            writer.WritePropertyName(kvp.Key);
            JsonSerializer.Serialize(writer, kvp.Value, kvp.Value?.GetType() ?? typeof(object), options);
        }
        writer.WriteEndObject();
    }
}
