using SyncApp.Helper;
using System.Text.Json.Serialization;

namespace SyncApp.Models;

public record TopDeskProgressTrailItem(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("memoText")] string MemoText,
    [property: JsonPropertyName("flag")] int Flag,
    [property: JsonPropertyName("invisibleForCaller")] bool InvisibleForCaller,
    [property: JsonConverter(typeof(TopDeskDateTimeConverter))]
    [property: JsonPropertyName("entryDate")] DateTime? EntryDate,
    [property: JsonPropertyName("operator")] Operator? Operator,
    [property: JsonPropertyName("person")] Operator? Person,
    [property: JsonPropertyName("plainText")] string PlainText,
    [property: JsonConverter(typeof(TopDeskDateTimeConverter))]
    [property: JsonPropertyName("creationDate")] DateTime? CreationDate
    );
