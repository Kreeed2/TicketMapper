using RestSharp;
using RestSharp.Authenticators;
using SyncApp.Helper;
using SyncApp.Interfaces;
using SyncApp.Models;
using SyncApp.Models.TopDesk;

namespace SyncApp.Services.TopDesk;

/// <summary>
/// Client for interacting with the TopDesk API to fetch and manage incidents, changes, and change activities.
/// </summary>
public class TopDeskClient(SystemConfig config, IRestClient httpClient, ILogger<TopDeskClient> logger) : ISystemClient
{
    /// <summary>
    /// Creates a base RestRequest with authentication for TopDesk API calls.
    /// </summary>
    /// <param name="pUrlPath">The API URL endpoint path to append to the base URL.</param>
    /// <remarks>Uses HTTP Basic Auth with provided username and password from configuration.</remarks>
    /// <returns>A configured RestRequest with basic authentication, or null if configuration is missing.</returns>
    private RestRequest? CreateBaseRequest(string pUrlPath)
    {
        if (Uri.TryCreate(config.Url, UriKind.Absolute, out var baseUrl)
            && !string.IsNullOrEmpty(config.Username)
            && !string.IsNullOrEmpty(config.Password))
        {
            var request = new RestRequest(new Uri(baseUrl, pUrlPath))
            {
                Authenticator = new HttpBasicAuthenticator(config.Username, config.Password)
            };

            return request;
        }
        return null;
    }

    /// <summary>
    /// Validates whether a given field exists on a specified TopDesk object type.
    /// </summary>
    /// <param name="objectType">The type of TopDesk object (e.g., "incident", "operatorchange").</param>
    /// <param name="fieldName">The name of the custom field to validate.</param>
    /// <remarks>Currently returns true by default as TopDesk doesn't support field metadata queries.</remarks>
    /// <returns>True if the field exists, false otherwise.</returns>
    public async Task<bool> ValidateFieldExistsAsync(string objectType, string fieldName)
    {
        logger.LogInformation("Validating field {fieldName} on TopDesk object {objectType}.", fieldName, objectType);
        return await Task.FromResult(true);
    }

    /// <summary>
    /// Retrieves a collection of items from TopDesk based on the specified object type.
    /// </summary>
    /// <param name="objectType">The type of TopDesk object to fetch ("incident", "operatorchange", or "operatorchangeactivity").</param>
    /// <returns>An enumerable collection of SyncItem objects representing the fetched data.</returns>
    /// <exception cref="ArgumentException">Thrown when an unsupported objectType is provided.</exception>
    public async Task<IEnumerable<SyncItem>> GetChangesAsync(string objectType)
    {
        return objectType.ToLowerInvariant() switch
        {
            "incident" => await GetIncidentsAsync(),
            "operatorchange" => await GetOperatorChangesAsync(),
            "operatorchangeactivity" => await GetOperatorChangeActivitiesAsync(),
            _ => throw new ArgumentException($"Unsupported TopDesk objectType: {objectType}", nameof(objectType))
        };
    }

    #region Incidents

    /// <summary>
    /// Fetches all incidents from the TopDesk API that match the configured query criteria.
    /// </summary>
    /// <remarks>Filters by category "Zesa" and processing status, sorted by modification date descending.</remarks>
    /// <returns>An enumerable collection of SyncItem objects representing incidents.</returns>
    private async Task<IEnumerable<SyncItem>> GetIncidentsAsync()
    {
        try
        {
            logger.LogInformation("Fetching incidents from TopDesk API");
            var request = CreateBaseRequest("/tas/api/incidents");

            if (request is null)
            {
                logger.LogError("Error fetching incidents from TopDesk: Request could not be created.");
                return [];
            }

            request.AddQueryParameter("sort", "modificationDate:desc");
            request.AddQueryParameter("query", "category.name==Zesa;processingStatus.id==160932da-84fb-5bb0-942a-e6be6e1f20e1");

            var response = await httpClient.ExecuteAsync<IEnumerable<Incident>>(request);

            if (response.IsSuccessStatusCode && response.Data is not null)
            {
                var syncItems = new List<SyncItem>();
                foreach (var incident in response.Data)
                {
                    var syncItem = MapIncidentToSyncItem(incident);
                    var requests = await GetRequestsAsync(incident.Id);
                    var operators = await GetOperatorAsync(incident.Operator.Id);
                    var progressTrail = await GetProgressTrail(incident.Id);

                    syncItem.Fields["report"] = FormatRequests(requests);
                    syncItem.ForeignFields["progressTrail"] = FormatProgressTails(progressTrail);

                    syncItems.Add(syncItem);
                }
                return syncItems;
            }
            logger.LogError("Error fetching incidents from TopDesk. Status: {Status}, Error: {Error}", response.StatusCode, response.ErrorMessage);
            return [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching incidents from TopDesk");
        }

        return [];
    }

    #endregion Incidents

    #region Operator Changes

    /// <summary>
    /// Fetches all operator changes from the TopDesk API.
    /// </summary>
    /// <remarks>Retrieves changes in batches of 50 and includes their associated activities.</remarks>
    /// <returns>An enumerable collection of SyncItem objects representing operator changes.</returns>
    private async Task<IEnumerable<SyncItem>> GetOperatorChangesAsync()
    {
        try
        {
            logger.LogInformation("Fetching operator changes from TopDesk API");
            var request = CreateBaseRequest("/tas/api/operatorChanges");

            if (request is null)
            {
                logger.LogError("Error fetching operator changes from TopDesk: Request could not be created.");
                return [];
            }

            request.AddQueryParameter("pageSize", "50");
            request.AddQueryParameter("sort", "modificationDate:desc");

            var response = await httpClient.ExecuteAsync<IEnumerable<TopDeskChange>>(request);

            if (response.IsSuccessStatusCode && response.Data is not null)
            {
                var changes = response.Data;
                var syncItems = new List<SyncItem>();
                foreach (var change in changes)
                {
                    var syncItem = MapChangeToSyncItem(change);

                    // Fetch activities belonging to this change
                    var changeId = change.Id;
                    if (!string.IsNullOrEmpty(changeId))
                    {
                        var activities = await GetChangeActivitiesForChangeAsync(changeId);
                        syncItem.ForeignFields["changeActivities"] = activities;
                    }

                    syncItems.Add(syncItem);
                }
                return syncItems;
            }

            logger.LogError("Error fetching operator changes from TopDesk. Status: {Status}, Error: {Error}",
                response.StatusCode, response.ErrorMessage);
            return [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching operator changes from TopDesk");
        }
        return [];
    }

    /// <summary>
    /// Fetches all change activities linked to a specific operator change.
    /// </summary>
    /// <param name="changeId">The ID of the operator change.</param>
    /// <returns>An enumerable collection of SyncItem objects representing change activities.</returns>
    private async Task<IEnumerable<SyncItem>> GetChangeActivitiesForChangeAsync(string changeId)
    {
        try
        {
            var request = CreateBaseRequest($"/tas/api/operatorChanges/{changeId}/operatorChangeActivities");
            if (request == null) return [];

            var response = await httpClient.ExecuteAsync<IEnumerable<TopDeskChangeActivity>>(request);

            if (response.IsSuccessStatusCode && response.Data is not null)
            {
                return [.. response.Data.Select(MapChangeActivityToSyncItem)];
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching change activities for change {changeId}", changeId);
        }
        return [];
    }

    #endregion Operator Changes

    #region Operator Change Activities

    /// <summary>
    /// Fetches all operator change activities from the TopDesk API.
    /// </summary>
    /// <remarks>Retrieves all change activities in batches of 50, sorted by modification date.</remarks>
    /// <returns>An enumerable collection of SyncItem objects representing operator change activities.</returns>
    private async Task<IEnumerable<SyncItem>> GetOperatorChangeActivitiesAsync()
    {
        try
        {
            logger.LogInformation("Fetching operator change activities from TopDesk API");
            var request = CreateBaseRequest("/tas/api/operatorChangeActivities");

            if (request == null)
            {
                logger.LogError("Error fetching operator change activities from TopDesk: Request could not be created.");
                return [];
            }

            request.AddQueryParameter("pageSize", "50");
            request.AddQueryParameter("sort", "modificationDate:desc");

            var response = await httpClient.ExecuteAsync<IEnumerable<TopDeskChangeActivity>>(request);

            if (response.IsSuccessStatusCode && response.Data is not null)
            {
                return [.. response.Data.Select(MapChangeActivityToSyncItem)];
            }

            logger.LogError("Error fetching operator change activities from TopDesk. Status: {Status}, Error: {Error}",
                response.StatusCode, response.ErrorMessage);
            return [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching operator change activities from TopDesk");
        }
        return [];
    }

    #endregion Operator Change Activities

    /// <summary>
    /// Searches for a TopDesk item by its external ID field value.
    /// </summary>
    /// <param name="objectType">The type of TopDesk object to find (e.g., "incident").</param>
    /// <param name="externalIdField">The name of the custom field containing unique external ID.</param>
    /// <param name="externalIdValue">The value of the external ID to search for.</param>
    /// <remarks>Currently returns null as this is a stub implementation.</remarks>
    /// <returns>A SyncItem if found, or null if not found.</returns>
    public async Task<SyncItem?> GetItemByExternalIdAsync(string objectType, string externalIdField, string externalIdValue)
    {
        logger.LogInformation("Searching TopDesk {objectType} for {externalIdField} = {externalIdValue}", objectType, externalIdField, externalIdValue);
        return await Task.FromResult<SyncItem?>(null);
    }

    /// <summary>
    /// Creates a new item in TopDesk with the specified object type and external ID.
    /// </summary>
    /// <param name="objectType">The type of TopDesk object to create.</param>
    /// <param name="item">The SyncItem containing field values for creating the object.</param>
    /// <param name="externalIdField">The name of the custom External ID field.</param>
    /// <param name="externalIdValue">The value to assign to the External ID field.</param>
    /// <remarks>Currently a stub implementation that returns a placeholder ID.</remarks>
    /// <returns>A string representing the newly created item's ID.</returns>
    public async Task<string> CreateItemAsync(string objectType, SyncItem item, string externalIdField, string externalIdValue)
    {
        logger.LogInformation("Creating TopDesk {objectType}", objectType);
        return await Task.FromResult("NEW_ID");
    }

    /// <summary>
    /// Updates an existing item in TopDesk with new field values.
    /// </summary>
    /// <param name="objectType">The type of TopDesk object to update.</param>
    /// <param name="id">The ID of the item to update.</param>
    /// <param name="item">The SyncItem containing updated field values.</param>
    /// <remarks>Currently a stub implementation.</remarks>
    public async Task UpdateItemAsync(string objectType, string id, SyncItem item)
    {
        logger.LogInformation("Updating TopDesk {objectType} {id}", objectType, id);
        throw new NotImplementedException();
    }

    /// <summary>
    /// Maps a TopDesk Incident object to a generic SyncItem for internal processing.
    /// </summary>
    /// <param name="incident">The Incident object from the TopDesk API.</param>
    /// <returns>A SyncItem containing mapped field values from the incident.</returns>
    private static SyncItem MapIncidentToSyncItem(Incident incident)
    {
        var fields = new Dictionary<string, object>();

        // Primitive fields
        Utilities.AddIfNotNull(fields, "id", incident.Id);
        Utilities.AddIfNotNull(fields, "status", incident.Status);
        Utilities.AddIfNotNull(fields, "number", incident.Number);
        Utilities.AddIfNotNull(fields, "briefDescription", incident.BriefDescription);
        Utilities.AddIfNotNull(fields, "externalNumber", incident.ExternalNumber);
        Utilities.AddIfNotNull(fields, "actualDuration", incident.ActualDuration);
        Utilities.AddIfNotNull(fields, "targetDate", incident.TargetDate);
        Utilities.AddIfNotNull(fields, "onHold", incident.OnHold);
        Utilities.AddIfNotNull(fields, "onHoldDuration", incident.OnHoldDuration);
        Utilities.AddIfNotNull(fields, "responded", incident.Responded);
        Utilities.AddIfNotNull(fields, "completed", incident.Completed);
        Utilities.AddIfNotNull(fields, "completedDate", incident.CompletedDate);
        Utilities.AddIfNotNull(fields, "closed", incident.Closed);
        Utilities.AddIfNotNull(fields, "closedDate", incident.ClosedDate);
        Utilities.AddIfNotNull(fields, "timeSpent", incident.TimeSpent);
        Utilities.AddIfNotNull(fields, "timeSpentFirstLine", incident.TimeSpentFirstLine);
        Utilities.AddIfNotNull(fields, "timeSpentSecondLine", incident.TimeSpentSecondLine);
        Utilities.AddIfNotNull(fields, "itemCosts", incident.ItemCosts);
        Utilities.AddIfNotNull(fields, "objectCosts", incident.ObjectCosts);
        Utilities.AddIfNotNull(fields, "costs", incident.Costs);
        Utilities.AddIfNotNull(fields, "callDate", incident.CallDate);
        Utilities.AddIfNotNull(fields, "creationDate", incident.CreationDate);
        Utilities.AddIfNotNull(fields, "modificationDate", incident.ModificationDate);
        Utilities.AddIfNotNull(fields, "majorCall", incident.MajorCall);
        Utilities.AddIfNotNull(fields, "publishToSsd", incident.PublishToSsd);
        Utilities.AddIfNotNull(fields, "monitored", incident.Monitored);
        Utilities.AddIfNotNull(fields, "expectedTimeSpent", incident.ExpectedTimeSpent);

        // Linked objects (ID + Name only)
        Utilities.AddIfNotNull(fields, "category.id", incident.Category?.Id);
        Utilities.AddIfNotNull(fields, "category.name", incident.Category?.Name);
        Utilities.AddIfNotNull(fields, "subcategory.id", incident.Subcategory?.Id);
        Utilities.AddIfNotNull(fields, "subcategory.name", incident.Subcategory?.Name);
        Utilities.AddIfNotNull(fields, "priority.id", incident.Priority?.Id);
        Utilities.AddIfNotNull(fields, "priority.name", incident.Priority?.Name);
        Utilities.AddIfNotNull(fields, "duration.id", incident.Duration?.Id);
        Utilities.AddIfNotNull(fields, "duration.name", incident.Duration?.Name);
        Utilities.AddIfNotNull(fields, "operator.id", incident.Operator?.Id);
        Utilities.AddIfNotNull(fields, "operator.name", incident.Operator?.Name);
        Utilities.AddIfNotNull(fields, "operatorGroup.id", incident.OperatorGroup?.Id);
        Utilities.AddIfNotNull(fields, "operatorGroup.name", incident.OperatorGroup?.Name);
        Utilities.AddIfNotNull(fields, "processingStatus.id", incident.ProcessingStatus?.Id);
        Utilities.AddIfNotNull(fields, "processingStatus.name", incident.ProcessingStatus?.Name);
        Utilities.AddIfNotNull(fields, "caller.id", incident.Caller?.Id);
        Utilities.AddIfNotNull(fields, "caller.name", incident.Caller?.DynamicName);
        Utilities.AddIfNotNull(fields, "caller.email", incident.Caller?.Email);
        Utilities.AddIfNotNull(fields, "creator.id", incident.Creator?.Id);
        Utilities.AddIfNotNull(fields, "creator.name", incident.Creator?.Name);
        Utilities.AddIfNotNull(fields, "modifier.id", incident.Modifier?.Id);
        Utilities.AddIfNotNull(fields, "modifier.name", incident.Modifier?.Name);

        // Optional Fields (as needed)
        AddOptionalFields(fields, incident.OptionalFields1, "optionalFields1");
        AddOptionalFields(fields, incident.OptionalFields2, "optionalFields2");

        return new SyncItem
        {
            Id = incident.Id,
            Fields = fields
        };
    }

    /// <summary>
    /// Adds optional field values from an OptionalFields object to a dictionary with a specified prefix.
    /// </summary>
    /// <param name="dict">The dictionary to populate with optional fields.</param>
    /// <param name="fields">The OptionalFields object containing optional data.</param>
    /// <param name="prefix">The prefix to prepend to each field name (e.g., "optionalFields1").</param>
    private static void AddOptionalFields(Dictionary<string, object> dict, OptionalFields? fields, string prefix)
    {
        if (fields == null) return;

        Utilities.AddIfNotNull(dict, $"{prefix}.boolean1", fields.Boolean1);
        Utilities.AddIfNotNull(dict, $"{prefix}.boolean2", fields.Boolean2);
        Utilities.AddIfNotNull(dict, $"{prefix}.number1", fields.Number1);
        Utilities.AddIfNotNull(dict, $"{prefix}.number2", fields.Number2);
        Utilities.AddIfNotNull(dict, $"{prefix}.text1", fields.Text1);
        Utilities.AddIfNotNull(dict, $"{prefix}.text2", fields.Text2);
        Utilities.AddIfNotNull(dict, $"{prefix}.text3", fields.Text3);
        Utilities.AddIfNotNull(dict, $"{prefix}.text4", fields.Text4);
        Utilities.AddIfNotNull(dict, $"{prefix}.text5", fields.Text5);
    }

    /// <summary>
    /// Maps a TopDesk Change object to a SyncItem.
    /// </summary>
    /// <param name="change">The TopDeskChange representing a change from the TopDesk API.</param>
    /// <returns>A SyncItem containing mapped field values from the change.</returns>
    private static SyncItem MapChangeToSyncItem(TopDeskChange change)
    {
        var fields = new Dictionary<string, object>();

        // Scalar fields
        Utilities.AddIfNotNull(fields, "id", change.Id);
        Utilities.AddIfNotNull(fields, "number", change.Number);
        Utilities.AddIfNotNull(fields, "briefDescription", change.BriefDescription);
        Utilities.AddIfNotNull(fields, "status", change.Status);
        Utilities.AddIfNotNull(fields, "externalNumber", change.ExternalNumber);
        Utilities.AddIfNotNull(fields, "creationDate", change.CreationDate);
        Utilities.AddIfNotNull(fields, "modificationDate", change.ModificationDate);
        Utilities.AddIfNotNull(fields, "closed", change.Closed);
        Utilities.AddIfNotNull(fields, "closedDate", change.ClosedDate);
        Utilities.AddIfNotNull(fields, "plannedStartDate", change.PlannedStartDate);
        Utilities.AddIfNotNull(fields, "plannedFinalDate", change.PlannedFinalDate);
        Utilities.AddIfNotNull(fields, "actualStartDate", change.ActualStartDate);
        Utilities.AddIfNotNull(fields, "actualFinalDate", change.ActualFinalDate);

        // Nested reference objects (id + name)
        Utilities.AddIfNotNull(fields, "category.id", change.Category?.Id);
        Utilities.AddIfNotNull(fields, "category.name", change.Category?.Name);
        Utilities.AddIfNotNull(fields, "subcategory.id", change.Subcategory?.Id);
        Utilities.AddIfNotNull(fields, "subcategory.name", change.Subcategory?.Name);
        Utilities.AddIfNotNull(fields, "priority.id", change.Priority?.Id);
        Utilities.AddIfNotNull(fields, "priority.name", change.Priority?.Name);
        Utilities.AddIfNotNull(fields, "changeType.id", change.ChangeType?.Id);
        Utilities.AddIfNotNull(fields, "changeType.name", change.ChangeType?.Name);
        Utilities.AddIfNotNull(fields, "impact.id", change.Impact?.Id);
        Utilities.AddIfNotNull(fields, "impact.name", change.Impact?.Name);
        Utilities.AddIfNotNull(fields, "benefit.id", change.Benefit?.Id);
        Utilities.AddIfNotNull(fields, "benefit.name", change.Benefit?.Name);
        Utilities.AddIfNotNull(fields, "template.id", change.Template?.Id);
        Utilities.AddIfNotNull(fields, "template.name", change.Template?.Name);
        Utilities.AddIfNotNull(fields, "operatorGroup.id", change.OperatorGroup?.Id);
        Utilities.AddIfNotNull(fields, "operatorGroup.name", change.OperatorGroup?.Name);
        Utilities.AddIfNotNull(fields, "operator.id", change.Operator?.Id);
        Utilities.AddIfNotNull(fields, "operator.name", change.Operator?.Name);
        Utilities.AddIfNotNull(fields, "creator.id", change.Creator?.Id);
        Utilities.AddIfNotNull(fields, "creator.name", change.Creator?.Name);
        Utilities.AddIfNotNull(fields, "modifier.id", change.Modifier?.Id);
        Utilities.AddIfNotNull(fields, "modifier.name", change.Modifier?.Name);

        return new SyncItem
        {
            Id = change.Id ?? string.Empty,
            Fields = fields
        };
    }

    /// <summary>
    /// Maps a TopDesk Change Activity object to a SyncItem.
    /// </summary>
    /// <param name="activity">The TopDeskChangeActivity representing a change activity from the TopDesk API.</param>
    /// <returns>A SyncItem containing mapped field values from the change activity.</returns>
    private static SyncItem MapChangeActivityToSyncItem(TopDeskChangeActivity activity)
    {
        var fields = new Dictionary<string, object>();

        // Scalar fields
        Utilities.AddIfNotNull(fields, "id", activity.Id);
        Utilities.AddIfNotNull(fields, "changeId", activity.ChangeId);
        Utilities.AddIfNotNull(fields, "briefDescription", activity.BriefDescription);
        Utilities.AddIfNotNull(fields, "status", activity.Status);
        Utilities.AddIfNotNull(fields, "plannedStartDate", activity.PlannedStartDate);
        Utilities.AddIfNotNull(fields, "plannedFinalDate", activity.PlannedFinalDate);
        Utilities.AddIfNotNull(fields, "actualStartDate", activity.ActualStartDate);
        Utilities.AddIfNotNull(fields, "actualFinalDate", activity.ActualFinalDate);
        Utilities.AddIfNotNull(fields, "creationDate", activity.CreationDate);
        Utilities.AddIfNotNull(fields, "modificationDate", activity.ModificationDate);
        Utilities.AddIfNotNull(fields, "closed", activity.Closed);
        Utilities.AddIfNotNull(fields, "closedDate", activity.ClosedDate);

        // Nested reference objects
        Utilities.AddIfNotNull(fields, "category.id", activity.Category?.Id);
        Utilities.AddIfNotNull(fields, "category.name", activity.Category?.Name);
        Utilities.AddIfNotNull(fields, "subcategory.id", activity.Subcategory?.Id);
        Utilities.AddIfNotNull(fields, "subcategory.name", activity.Subcategory?.Name);
        Utilities.AddIfNotNull(fields, "operatorGroup.id", activity.OperatorGroup?.Id);
        Utilities.AddIfNotNull(fields, "operatorGroup.name", activity.OperatorGroup?.Name);
        Utilities.AddIfNotNull(fields, "operator.id", activity.Operator?.Id);
        Utilities.AddIfNotNull(fields, "operator.name", activity.Operator?.Name);
        Utilities.AddIfNotNull(fields, "assignee.id", activity.Assignee?.Id);
        Utilities.AddIfNotNull(fields, "assignee.name", activity.Assignee?.Name);
        Utilities.AddIfNotNull(fields, "creator.id", activity.Creator?.Id);
        Utilities.AddIfNotNull(fields, "creator.name", activity.Creator?.Name);
        Utilities.AddIfNotNull(fields, "modifier.id", activity.Modifier?.Id);
        Utilities.AddIfNotNull(fields, "modifier.name", activity.Modifier?.Name);

        return new SyncItem
        {
            Id = activity.Id ?? string.Empty,
            Fields = fields
        };
    }

    /// <summary>
    /// Fetches all requests (notes/comments) associated with a specific incident.
    /// </summary>
    /// <param name="incidentId">The ID of the incident.</param>
    /// <returns>An enumerable collection of TopDeskRequest objects.</returns>
    private async Task<IEnumerable<TopDeskRequest>> GetRequestsAsync(string incidentId)
    {
        try
        {
            var request = CreateBaseRequest($"/tas/api/incidents/id/{incidentId}/requests");
            if (request == null) return [];

            var response = await httpClient.ExecuteAsync<IEnumerable<TopDeskRequest>>(request);
            if (response.IsSuccessStatusCode && response.Data is not null)
            {
                return response.Data;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching requests for incident {incidentId}", incidentId);
        }
        return [];
    }

    /// <summar>
    /// Fetches detailed operator information by operator ID.
    /// </summary>
    /// <param name="pOperatorId">The ID of the operator.</param>
    /// <returns>An OperatorFull object containing operator details, or null if not found.</returns>
    /// <remarks>Used to enrich incident data with operator details.</remarks>
    private async Task<OperatorFull?> GetOperatorAsync(string pOperatorId)
    {
        try
        {
            var request = CreateBaseRequest($"/tas/api/operators/id/{pOperatorId}");
            if (request == null) return null;

            var response = await httpClient.ExecuteAsync<OperatorFull>(request);
            if (response.IsSuccessStatusCode && response.Data is not null)
            {
                return response.Data;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching operator {pOperatorId}", pOperatorId);
        }
        return null;
    }

    /// <summary>
    /// Fetches the progress trail (history log) for a specific incident.
    /// </summary>
    /// <param name="pIncidentId">The ID of the incident.</param>
    /// <returns>An enumerable collection of TopDeskProgressTrailItem objects.</returns>
    public async Task<IEnumerable<TopDeskProgressTrailItem>> GetProgressTrail(string pIncidentId)
    {
        try
        {
            var request = CreateBaseRequest($"/tas/api/incidents/id/{pIncidentId}/progresstrail");
            if (request == null) return [];

            var response = await httpClient.ExecuteAsync<IEnumerable<TopDeskProgressTrailItem>>(request);
            if (response.IsSuccessStatusCode && response.Data is not null)
            {
                return response.Data;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching progress trail for {pIncidentId}", pIncidentId);
        }
        return [];
    }

    /// <summary>
    /// Formats a collection of TopDesk Requests into an HTML-formatted string.
    /// </summary>
    /// <param name="requests">The collection of requests to form.</param>
    /// <returns>An HTML string with each request entry, including date, person name, and memo text.</returns>
    protected static string FormatRequests(IEnumerable<TopDeskRequest> requests)
    {
        if (requests == null || !requests.Any()) return string.Empty;

        var sb = new System.Text.StringBuilder();
        foreach (var req in requests.OrderBy(r => r.EntryDate))
        {
            sb.AppendLine($"<b>{req.EntryDate:yyyy-MM-dd HH:mm} - {req.Person?.Name ?? "Unknown"}</b><br>");
            sb.AppendLine(req.MemoText?.Replace("\n", "<br>") ?? "");
            sb.AppendLine("<br><hr><br>");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Converts a collection of Progress Trail items into a collection of SyncItem objects.
    /// </summary>
    /// <param name="pProgressTrailItems">The collection of progress trail items to convert.</param>
    /// <returns>An enumerable collection of SyncItem objects representing progress trail entries.</returns>
    protected static IEnumerable<SyncItem> FormatProgressTails(IEnumerable<TopDeskProgressTrailItem> pProgressTrailItems)
    {
        if (pProgressTrailItems == null || !pProgressTrailItems.Any()) return [];

        return pProgressTrailItems.Select(itm =>
        {
            var fields = new Dictionary<string, object>();

            Utilities.AddIfNotNull(fields, "memoText", itm.MemoText);
            Utilities.AddIfNotNull(fields, "plainText", itm.PlainText);
            Utilities.AddIfNotNull(fields, "operator.id", itm.Operator?.Id);
            Utilities.AddIfNotNull(fields, "operator.name", itm.Operator?.Name);
            Utilities.AddIfNotNull(fields, "person.id", itm.Person?.Id);
            Utilities.AddIfNotNull(fields, "person.name", itm.Person?.Name);
            Utilities.AddIfNotNull(fields, "flag", itm.Flag);
            Utilities.AddIfNotNull(fields, "entryDate", (object?)itm.EntryDate);
            Utilities.AddIfNotNull(fields, "creationDate", itm.CreationDate);

            return new SyncItem { Id = itm.Id, Fields = fields };
        });
    }
}
