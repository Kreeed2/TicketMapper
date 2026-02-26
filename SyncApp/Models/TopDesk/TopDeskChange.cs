using System.Text.Json.Serialization;
using SyncApp.Helper;

namespace SyncApp.Models.TopDesk;

public record TopDeskChangeResult(
    [property: JsonPropertyName("results")] IEnumerable<TopDeskChange> Results
);

public record TopDeskChange(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("number")] string Number,
    [property: JsonPropertyName("briefDescription")] string BriefDescription,
    [property: JsonPropertyName("status")] TopDeskTuple Status,
    [property: JsonPropertyName("externalNumber")] string ExternalNumber,
    [property: JsonPropertyName("changeType")] string ChangeType,
    [property: JsonPropertyName("creationDate")]
    [property: JsonConverter(typeof(TopDeskDateTimeConverter))]
    DateTime? CreationDate,
    [property: JsonPropertyName("modificationDate")]
    [property: JsonConverter(typeof(TopDeskDateTimeConverter))]
    DateTime? ModificationDate,
    [property: JsonPropertyName("closed")] bool? Closed,
    [property: JsonPropertyName("closedDate")]
    [property: JsonConverter(typeof(TopDeskDateTimeConverter))]
    DateTime? ClosedDate,
    [property: JsonPropertyName("plannedStartDate")]
    [property: JsonConverter(typeof(TopDeskDateTimeConverter))]
    DateTime? PlannedStartDate,
    [property: JsonPropertyName("plannedFinalDate")]
    [property: JsonConverter(typeof(TopDeskDateTimeConverter))]
    DateTime? PlannedFinalDate,
    [property: JsonPropertyName("actualStartDate")]
    [property: JsonConverter(typeof(TopDeskDateTimeConverter))]
    DateTime? ActualStartDate,
    [property: JsonPropertyName("actualFinalDate")]
    [property: JsonConverter(typeof(TopDeskDateTimeConverter))]
    DateTime? ActualFinalDate,
    [property: JsonPropertyName("category")] TopDeskTuple Category,
    [property: JsonPropertyName("subcategory")] TopDeskTuple Subcategory,
    [property: JsonPropertyName("priority")] TopDeskTuple Priority,
    [property: JsonPropertyName("impact")] TopDeskTuple Impact,
    [property: JsonPropertyName("benefit")] TopDeskTuple Benefit,
    [property: JsonPropertyName("template")] TopDeskTuple Template,
    [property: JsonPropertyName("operatorGroup")] TopDeskTuple OperatorGroup,
    [property: JsonPropertyName("operator")] TopDeskTuple Operator,
    [property: JsonPropertyName("creator")] TopDeskTuple Creator,
    [property: JsonPropertyName("modifier")] TopDeskTuple Modifier
);
