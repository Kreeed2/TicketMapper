using System.Text.Json.Serialization;
using SyncApp.Helper;

namespace SyncApp.Models.TopDesk;

public record Branch(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("clientReferenceNumber")] string ClientReferenceNumber,
    [property: JsonPropertyName("timeZone")] string TimeZone,
    [property: JsonPropertyName("extraA")] object ExtraA,
    [property: JsonPropertyName("extraB")] object ExtraB
);

public record Caller(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("dynamicName")] string DynamicName,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("phoneNumber")] string PhoneNumber,
    [property: JsonPropertyName("mobileNumber")] string MobileNumber,
    [property: JsonPropertyName("branch")] Branch Branch,
    [property: JsonPropertyName("department")] TopDeskTuple Department,
    [property: JsonPropertyName("budgetHolder")] TopDeskTuple BudgetHolder
);

public record CallerBranch(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("clientReferenceNumber")] string ClientReferenceNumber,
    [property: JsonPropertyName("timeZone")] string TimeZone,
    [property: JsonPropertyName("extraA")] object ExtraA,
    [property: JsonPropertyName("extraB")] object ExtraB
);

public record OptionalFields(
    [property: JsonPropertyName("boolean1")] bool? Boolean1,
    [property: JsonPropertyName("boolean2")] bool? Boolean2,
    [property: JsonPropertyName("boolean3")] bool? Boolean3,
    [property: JsonPropertyName("boolean4")] bool? Boolean4,
    [property: JsonPropertyName("boolean5")] bool? Boolean5,
    [property: JsonPropertyName("number1")] double? Number1,
    [property: JsonPropertyName("number2")] double? Number2,
    [property: JsonPropertyName("number3")] double? Number3,
    [property: JsonPropertyName("number4")] double? Number4,
    [property: JsonPropertyName("number5")] double? Number5,
    [property: JsonPropertyName("date1")] object Date1,
    [property: JsonPropertyName("date2")] object Date2,
    [property: JsonPropertyName("date3")] object Date3,
    [property: JsonPropertyName("date4")] object Date4,
    [property: JsonPropertyName("date5")] object Date5,
    [property: JsonPropertyName("text1")] string Text1,
    [property: JsonPropertyName("text2")] string Text2,
    [property: JsonPropertyName("text3")] string Text3,
    [property: JsonPropertyName("text4")] string Text4,
    [property: JsonPropertyName("text5")] string Text5,
    [property: JsonPropertyName("memo1")] object Memo1,
    [property: JsonPropertyName("memo2")] object Memo2,
    [property: JsonPropertyName("memo3")] object Memo3,
    [property: JsonPropertyName("memo4")] object Memo4,
    [property: JsonPropertyName("memo5")] object Memo5,
    [property: JsonPropertyName("searchlist1")] object Searchlist1,
    [property: JsonPropertyName("searchlist2")] object Searchlist2,
    [property: JsonPropertyName("searchlist3")] object Searchlist3,
    [property: JsonPropertyName("searchlist4")] object Searchlist4,
    [property: JsonPropertyName("searchlist5")] object Searchlist5
);

public record Incident(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("number")] string Number,
    [property: JsonPropertyName("request")] string Request,
    [property: JsonPropertyName("requests")] string Requests,
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("attachments")] string Attachments,
    [property: JsonPropertyName("caller")] Caller Caller,
    [property: JsonPropertyName("callerBranch")] CallerBranch CallerBranch,
    [property: JsonPropertyName("callerLocation")] TopDeskTuple CallerLocation,
    [property: JsonPropertyName("branchExtraFieldA")] object BranchExtraFieldA,
    [property: JsonPropertyName("branchExtraFieldB")] object BranchExtraFieldB,
    [property: JsonPropertyName("briefDescription")] string BriefDescription,
    [property: JsonPropertyName("externalNumber")] string ExternalNumber,
    [property: JsonPropertyName("category")] TopDeskTuple Category,
    [property: JsonPropertyName("subcategory")] TopDeskTuple Subcategory,
    [property: JsonPropertyName("callType")] object CallType,
    [property: JsonPropertyName("entryType")] TopDeskTuple EntryType,
    [property: JsonPropertyName("object")] object Object,
    [property: JsonPropertyName("asset")] object Asset,
    [property: JsonPropertyName("branch")] object Branch,
    [property: JsonPropertyName("location")] object Location,
    [property: JsonPropertyName("impact")] object Impact,
    [property: JsonPropertyName("urgency")] object Urgency,
    [property: JsonPropertyName("priority")] TopDeskTuple Priority,
    [property: JsonPropertyName("duration")] TopDeskTuple Duration,
    [property: JsonPropertyName("actualDuration")] int? ActualDuration,
    [property: JsonPropertyName("targetDate")]
    [property: JsonConverter(typeof(TopDeskDateTimeConverter))]
    DateTime? TargetDate,
    [property: JsonPropertyName("sla")] object Sla,
    [property: JsonPropertyName("onHold")] bool? OnHold,
    [property: JsonPropertyName("onHoldDate")] object OnHoldDate,
    [property: JsonPropertyName("onHoldDuration")] int? OnHoldDuration,
    [property: JsonPropertyName("feedbackMessage")] object FeedbackMessage,
    [property: JsonPropertyName("feedbackRating")] object FeedbackRating,
    [property: JsonPropertyName("operator")] TopDeskTuple Operator,
    [property: JsonPropertyName("operatorGroup")] TopDeskTuple OperatorGroup,
    [property: JsonPropertyName("supplier")] object Supplier,
    [property: JsonPropertyName("processingStatus")] TopDeskTuple ProcessingStatus,
    [property: JsonPropertyName("responded")] bool? Responded,
    [property: JsonPropertyName("responseDate")] object ResponseDate,
    [property: JsonPropertyName("completed")] bool? Completed,
    [property: JsonPropertyName("completedDate")]
    [property: JsonConverter(typeof(TopDeskDateTimeConverter))]
    DateTime? CompletedDate,
    [property: JsonPropertyName("closed")] bool? Closed,
    [property: JsonPropertyName("closedDate")]
    [property: JsonConverter(typeof(TopDeskDateTimeConverter))]
    DateTime? ClosedDate,
    [property: JsonPropertyName("closureCode")] object ClosureCode,
    [property: JsonPropertyName("timeSpent")] int? TimeSpent,
    [property: JsonPropertyName("timeSpentFirstLine")] int? TimeSpentFirstLine,
    [property: JsonPropertyName("timeSpentSecondLine")] int? TimeSpentSecondLine,
    [property: JsonPropertyName("timeSpentPartial")] int? TimeSpentPartial,
    [property: JsonPropertyName("timeSpentLinkedPartials")] int? TimeSpentLinkedPartials,
    [property: JsonPropertyName("timeSpentSecondLineAndPartials")] int? TimeSpentSecondLineAndPartials,
    [property: JsonPropertyName("itemCosts")] double? ItemCosts,
    [property: JsonPropertyName("objectCosts")] double? ObjectCosts,
    [property: JsonPropertyName("costs")] double? Costs,
    [property: JsonPropertyName("escalationStatus")] object EscalationStatus,
    [property: JsonPropertyName("escalationReason")] object EscalationReason,
    [property: JsonPropertyName("escalationOperator")] object EscalationOperator,
    [property: JsonPropertyName("callDate")]
    [property: JsonConverter(typeof(TopDeskDateTimeConverter))]
    DateTime? CallDate,
    [property: JsonPropertyName("creator")] TopDeskTuple Creator,
    [property: JsonPropertyName("creationDate")]
    [property: JsonConverter(typeof(TopDeskDateTimeConverter))]
    DateTime? CreationDate,
    [property: JsonPropertyName("modifier")] TopDeskTuple Modifier,
    [property: JsonPropertyName("modificationDate")]
    [property: JsonConverter(typeof(TopDeskDateTimeConverter))]
    DateTime? ModificationDate,
    [property: JsonPropertyName("majorCall")] bool? MajorCall,
    [property: JsonPropertyName("majorCallObject")] object MajorCallObject,
    [property: JsonPropertyName("publishToSsd")] bool? PublishToSsd,
    [property: JsonPropertyName("monitored")] bool? Monitored,
    [property: JsonPropertyName("expectedTimeSpent")] int? ExpectedTimeSpent,
    [property: JsonPropertyName("archivingReason")] object ArchivingReason,
    [property: JsonPropertyName("mainIncident")] object MainIncident,
    [property: JsonPropertyName("optionalFields1")] OptionalFields OptionalFields1,
    [property: JsonPropertyName("optionalFields2")] OptionalFields OptionalFields2,
    [property: JsonPropertyName("externalLinks")] IReadOnlyList<object> ExternalLinks,
    [property: JsonPropertyName("partialIncidents")] IReadOnlyList<object> PartialIncidents
);

public record TopDeskRequest(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonConverter(typeof(TopDeskDateTimeConverter))]
    [property: JsonPropertyName("entryDate")] DateTime? EntryDate,
    [property: JsonPropertyName("memoText")] string MemoText,
    [property: JsonPropertyName("operator")] TopDeskTuple Operator,
    [property: JsonPropertyName("person")] TopDeskTuple Person,
    [property: JsonPropertyName("creationDate")]
    [property: JsonConverter(typeof(TopDeskDateTimeConverter))]
    DateTime? CreationDate,
    [property: JsonPropertyName("flag")] int? Flag
);

public record Location(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("branch")] Branch Branch,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("room")] string Room
);

public record TopDeskAttachment(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("fileName")] string FileName,
    [property: JsonPropertyName("size")] long Size
);