using System.Text.Json;
using System.Text.Json.Serialization;

namespace SyncApp.Helper;

public class TopDeskDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Unexpected token {reader.TokenType} when parsing DateTime");
        }

        var dateString = reader.GetString();
        if (string.IsNullOrEmpty(dateString))
        {
            return null;
        }

        // Format: "2025-02-24T14:05:00.000+0000"
        // Entferne den Offset am Ende
        if (dateString.Length > 6 && (dateString[^5] == '+' || dateString[^5] == '-'))
        {
            dateString = dateString[..^5];
        }

        if (DateTime.TryParseExact(
            dateString,
            "yyyy-MM-ddTHH:mm:ss.fff",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal,
            out DateTime result))
        {
            return result;
        }
        else if (DateTime.TryParseExact(
            dateString,
            "yyyy-MM-ddTHH:mm:ss",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal,
            out result))
        {
            return result;
        }

        throw new JsonException($"Unable to parse DateTime: {reader.GetString()}");
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString("yyyy-MM-ddTHH:mm:ss.fff+0000"));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}