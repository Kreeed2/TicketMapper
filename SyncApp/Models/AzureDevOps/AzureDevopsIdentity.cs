using System.Text.Json.Serialization;

namespace SyncApp.Models.AzureDevOps;

public record Account(
    [property: JsonPropertyName("$type")] string Type,
    [property: JsonPropertyName("$value")] string Value
);

public record ComplianceValidated(
    [property: JsonPropertyName("$type")] string Type,
    [property: JsonPropertyName("$value")] DateTime? Value
);

public record Description(
    [property: JsonPropertyName("$type")] string Type,
    [property: JsonPropertyName("$value")] string Value
);

public record DirectoryAlias(
    [property: JsonPropertyName("$type")] string Type,
    [property: JsonPropertyName("$value")] string Value
);

public record DN(
    [property: JsonPropertyName("$type")] string Type,
    [property: JsonPropertyName("$value")] string Value
);

public record Domain(
    [property: JsonPropertyName("$type")] string Type,
    [property: JsonPropertyName("$value")] string Value
);

public record Mail(
    [property: JsonPropertyName("$type")] string Type,
    [property: JsonPropertyName("$value")] string Value
);

public record Properties(
    [property: JsonPropertyName("SchemaClassName")] SchemaClassName SchemaClassName,
    [property: JsonPropertyName("Description")] Description Description,
    [property: JsonPropertyName("Domain")] Domain Domain,
    [property: JsonPropertyName("Account")] Account Account,
    [property: JsonPropertyName("DN")] DN DN,
    [property: JsonPropertyName("Mail")] Mail Mail,
    [property: JsonPropertyName("SpecialType")] SpecialType SpecialType,
    [property: JsonPropertyName("ComplianceValidated")] ComplianceValidated ComplianceValidated,
    [property: JsonPropertyName("DirectoryAlias")] DirectoryAlias DirectoryAlias
);

public record AzureDevopsIdentityRequest(
    [property: JsonPropertyName("count")] int? Count,
    [property: JsonPropertyName("value")] IReadOnlyList<AzureDevopsIdentity> Identities
);

public record SchemaClassName(
    [property: JsonPropertyName("$type")] string Type,
    [property: JsonPropertyName("$value")] string Value
);

public record SpecialType(
    [property: JsonPropertyName("$type")] string Type,
    [property: JsonPropertyName("$value")] string Value
);

public record AzureDevopsIdentity(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("descriptor")] string Descriptor,
    [property: JsonPropertyName("subjectDescriptor")] string SubjectDescriptor,
    [property: JsonPropertyName("providerDisplayName")] string ProviderDisplayName,
    [property: JsonPropertyName("isActive")] bool? IsActive,
    [property: JsonPropertyName("members")] IReadOnlyList<object> Members,
    [property: JsonPropertyName("memberOf")] IReadOnlyList<object> MemberOf,
    [property: JsonPropertyName("memberIds")] IReadOnlyList<object> MemberIds,
    [property: JsonPropertyName("masterId")] string MasterId,
    [property: JsonPropertyName("properties")] Properties Properties,
    [property: JsonPropertyName("resourceVersion")] int? ResourceVersion,
    [property: JsonPropertyName("metaTypeId")] int? MetaTypeId
);