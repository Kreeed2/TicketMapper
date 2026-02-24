using SyncApp.Helper;
using System.Text.Json.Serialization;

namespace SyncApp.Models.TopDesk;

public record TopDeskProgressTrailItem()
{
    /// <summary>
    /// Generated value to distinguish between email and normal progress trail item
    /// </summary>
    public bool IsEmail => MemoText != null;

    [property: JsonPropertyName("id")]
    public required string Id { get; init; }

    [property: JsonConverter(typeof(TopDeskDateTimeConverter))]
    [property: JsonPropertyName("entryDate")]
    public DateTime? EntryDate { get; init; }

    [property: JsonPropertyName("operator")]
    public Operator? Operator { get; init; }

    #region Normal progress trail item fields 

    [property: JsonPropertyName("memoText")]
    public string? MemoText { get; init; }

    [property: JsonPropertyName("flag")]
    public int? Flag { get; init; }

    [property: JsonPropertyName("invisibleForCaller")]
    public bool? InvisibleForCaller { get; init; }

    [property: JsonPropertyName("person")]
    public Operator? Person { get; init; }

    [property: JsonPropertyName("plainText")]
    public string? PlainText { get; init; }

    [property: JsonConverter(typeof(TopDeskDateTimeConverter))]
    [property: JsonPropertyName("creationDate")]
    public DateTime? CreationDate { get; init; }

    #endregion

    #region Email progress trail item fields

    [property: JsonPropertyName("title")]
    public string? Title { get; init; }

    [property: JsonPropertyName("sender")]
    public string? Sender { get; init; }
    
    [property: JsonPropertyName("details")]
    public string? Details { get; init; }

    [property: JsonPropertyName("user")]
    public Operator? User { get; init; }

    #endregion
}
