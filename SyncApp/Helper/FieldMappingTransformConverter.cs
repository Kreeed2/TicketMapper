using SyncApp.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SyncApp.Helper;

public class FieldMappingTransformConverter : JsonConverter<FieldMappingTransform>
{
    public override FieldMappingTransform Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString() ?? string.Empty;
        return s.ToLowerInvariant() switch
        {
            "none" => FieldMappingTransform.None,
            "lookup" => FieldMappingTransform.Lookup,
            "static" => FieldMappingTransform.Static,
            "html_to_markdown" => FieldMappingTransform.HtmlToMarkdown,
            "pattern" => FieldMappingTransform.Pattern,
            "value_map" => FieldMappingTransform.ValueMap,
            _ => throw new JsonException($"Unknown FieldMappingTransform '{s}'")
        };
    }

    public override void Write(Utf8JsonWriter writer, FieldMappingTransform value, JsonSerializerOptions options)
    {
        var s = value switch
        {
            FieldMappingTransform.None => "none",
            FieldMappingTransform.Lookup => "lookup",
            FieldMappingTransform.Static => "static",
            FieldMappingTransform.HtmlToMarkdown => "html_to_markdown",
            FieldMappingTransform.Pattern => "pattern",
            FieldMappingTransform.ValueMap => "value_map",
            _ => throw new JsonException($"Unknown FieldMappingTransform '{value}'")
        };
        writer.WriteStringValue(s);
    }
}
