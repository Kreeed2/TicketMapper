using System.Text.Json.Serialization;
using SyncApp.Helper;

namespace SyncApp.Models.TopDesk;

public record ChangeType(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name
);

public record Impact(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name
);

public record Benefit(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name
);

public record Template(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name
);

public record TopDeskChange(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("number")] string Number,
    [property: JsonPropertyName("briefDescription")] string BriefDescription,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("externalNumber")] string ExternalNumber,
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
    [property: JsonPropertyName("category")] Category Category,
    [property: JsonPropertyName("subcategory")] Subcategory Subcategory,
    [property: JsonPropertyName("priority")] Priority Priority,
    [property: JsonPropertyName("changeType")] ChangeType ChangeType,
    [property: JsonPropertyName("impact")] Impact Impact,
    [property: JsonPropertyName("benefit")] Benefit Benefit,
    [property: JsonPropertyName("template")] Template Template,
    [property: JsonPropertyName("operatorGroup")] OperatorGroup OperatorGroup,
    [property: JsonPropertyName("operator")] Operator Operator,
    [property: JsonPropertyName("creator")] Creator Creator,
    [property: JsonPropertyName("modifier")] Modifier Modifier
);
