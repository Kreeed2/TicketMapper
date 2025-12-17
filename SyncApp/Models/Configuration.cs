using System.Collections.Generic;
using Newtonsoft.Json;

namespace SyncApp.Models
{
    public record AppConfiguration
    {
        [JsonProperty("settings")]
        public SettingsConfig Settings { get; set; } = new SettingsConfig();

        [JsonProperty("systems")]
        public Dictionary<string, SystemConfig> Systems { get; set; } = [];

        [JsonProperty("mappings")]
        public List<MappingConfig> Mappings { get; set; } = [];
    }

    public record UserMappingItem
    {
        [JsonProperty("source")]
        public string Source { get; set; } = string.Empty;

        [JsonProperty("target")]
        public string Target { get; set; } = string.Empty;
    }

    public record SettingsConfig
    {
        [JsonProperty("run_mode")]
        public string RunMode { get; set; } = "polling";

        [JsonProperty("poll_interval_seconds")]
        public int PollIntervalSeconds { get; set; } = 30;
    }

    public record SystemConfig
    {
        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        [JsonProperty("url")]
        public string Url { get; set; } = string.Empty;

        [JsonProperty("auth_type")]
        public string AuthType { get; set; } = string.Empty;

        [JsonProperty("username")]
        public string? Username { get; set; }

        [JsonProperty("password")]
        public string? Password { get; set; }

        [JsonProperty("token")]
        public string? Token { get; set; }

        [JsonProperty("defaults")]
        public Dictionary<string, string> Defaults { get; set; } = [];
    }

    public record MappingConfig
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("source_system")]
        public string SourceSystem { get; set; } = string.Empty;

        [JsonProperty("target_system")]
        public string TargetSystem { get; set; } = string.Empty;

        [JsonProperty("source_object")]
        public string SourceObject { get; set; } = string.Empty;

        [JsonProperty("target_object")]
        public string TargetObject { get; set; } = string.Empty;

        [JsonProperty("external_id_field")]
        public string ExternalIdField { get; set; } = string.Empty;

        [JsonProperty("fields")]
        public List<FieldMapping> Fields { get; set; } = [];
    }

    public record FieldMapping
    {
        [JsonProperty("source")]
        public string Source { get; set; } = string.Empty;

        [JsonProperty("target")]
        public string Target { get; set; } = string.Empty;

        [JsonProperty("transform")]
        public FieldMappingTransform Transform { get; set; } = FieldMappingTransform.None;

        [JsonProperty("update")]
        public bool Update { get; set; } = true;

        [JsonProperty("pattern")]
        public string? Pattern { get; set; } = null;
    }

    public enum FieldMappingTransform
    {
        [JsonProperty("none")] None,
        [JsonProperty("lookup")] Lookup,
        [JsonProperty("static")] Static,
        [JsonProperty("html_to_markdown")] HtmlToMarkdown,
        [JsonProperty("pattern")] Pattern,
    }
}
