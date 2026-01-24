using SyncApp.Models;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SyncApp.Helper;

public class SystemMappingTypeConverter : JsonConverter<SystemMappingType>
{
    public override SystemMappingType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString() ?? string.Empty;
        return s.ToLowerInvariant() switch
        {
            "topdesk" => SystemMappingType.TopDesk,
            "azuredevops" => SystemMappingType.AzureDevOps,
            _ => throw new JsonException($"Unknown SystemMappingType '{s}'")
        };
    }

    public override void Write(Utf8JsonWriter writer, SystemMappingType value, JsonSerializerOptions options)
    {
        var s = value switch
        {
            SystemMappingType.TopDesk => "topdesk",
            SystemMappingType.AzureDevOps => "azuredevops",
            _ => throw new JsonException($"Unknown SystemMappingType '{value}'")
        };
        writer.WriteStringValue(s);
    }
}