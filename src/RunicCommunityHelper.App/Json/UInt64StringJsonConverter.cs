using System.Text.Json;
using System.Text.Json.Serialization;

namespace RunicCommunityHelper.Json;

public class UInt64StringJsonConverter : JsonConverter<ulong>
{
    public override ulong Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            return reader.GetUInt64();
        }

        return ulong.TryParse(reader.GetString(), out var value)
            ? value
            : throw new JsonException("Expected an unsigned 64-bit integer.");
    }

    public override void Write(Utf8JsonWriter writer, ulong value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
