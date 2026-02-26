using System.Text.Json.Serialization;

namespace SyncApp.Models.TopDesk;

public record TopDeskTuple(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name
);