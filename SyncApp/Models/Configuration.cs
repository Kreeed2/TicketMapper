using SyncApp.Helper;
using System.Text.Json.Serialization;

namespace SyncApp.Models
{
    public record AppConfiguration
    {
        [JsonPropertyName("settings")]
        public SettingsConfig Settings { get; set; } = new SettingsConfig();

        [JsonPropertyName("systems")]
        public Dictionary<string, SystemConfig> Systems { get; set; } = [];

        [JsonPropertyName("mappings")]
        public List<MappingConfig> Mappings { get; set; } = [];
    }

    public record UserMappingItem
    {
        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;

        [JsonPropertyName("target")]
        public string Target { get; set; } = string.Empty;
    }

    public record SettingsConfig
    {
        [JsonPropertyName("run_mode")]
        public string RunMode { get; set; } = "polling";

        [JsonPropertyName("poll_interval_seconds")]
        public int PollIntervalSeconds { get; set; } = 30;
    }

    public record SystemConfig
    {
        [JsonPropertyName("type")]
        public SystemMappingType Type { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("auth_type")]
        public string AuthType { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("password")]
        public string? Password { get; set; }

        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("defaults")]
        public Dictionary<string, string> Defaults { get; set; } = [];
    }

    public record MappingConfig
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("source_system")]
        public string SourceSystem { get; set; } = string.Empty;

        [JsonPropertyName("target_system")]
        public string TargetSystem { get; set; } = string.Empty;

        [JsonPropertyName("source_object")]
        public string SourceObject { get; set; } = string.Empty;

        [JsonPropertyName("target_object")]
        public string TargetObject { get; set; } = string.Empty;

        [JsonPropertyName("external_id_field")]
        public string ExternalIdField { get; set; } = string.Empty;

        [JsonPropertyName("fields")]
        public List<FieldMapping> Fields { get; set; } = [];
    }

    public record FieldMapping
    {
        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;

        [JsonPropertyName("target")]
        public string Target { get; set; } = string.Empty;

        [JsonPropertyName("transform")]
        public FieldMappingTransform Transform { get; set; } = FieldMappingTransform.None;

        [JsonPropertyName("update")]
        public bool Update { get; set; } = true;

        [JsonPropertyName("pattern")]
        public string? Pattern { get; set; } = null;
    }

    [JsonConverter(typeof(FieldMappingTransformConverter))]
    public enum FieldMappingTransform
    {
        None,
        Lookup,
        Static,
        HtmlToMarkdown,
        Pattern,
    }

    [JsonConverter(typeof(SystemMappingTypeConverter))]
    public enum SystemMappingType
    {
        TopDesk,
        AzureDevOps
    }
}
