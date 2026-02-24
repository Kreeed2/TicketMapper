using System.Text.Json.Serialization;
using SyncApp.Helper;

namespace SyncApp.Models.TopDesk;

public record Assignee(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name
);

public record TopDeskChangeActivity(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("changeId")] string ChangeId,
    [property: JsonPropertyName("briefDescription")] string BriefDescription,
    [property: JsonPropertyName("status")] string Status,
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
    [property: JsonPropertyName("category")] Category Category,
    [property: JsonPropertyName("subcategory")] Subcategory Subcategory,
    [property: JsonPropertyName("operatorGroup")] OperatorGroup OperatorGroup,
    [property: JsonPropertyName("operator")] Operator Operator,
    [property: JsonPropertyName("assignee")] Assignee Assignee,
    [property: JsonPropertyName("creator")] Creator Creator,
    [property: JsonPropertyName("modifier")] Modifier Modifier
);
