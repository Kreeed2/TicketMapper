using Microsoft.TeamFoundation.Build.WebApi;
using RestSharp;
using RestSharp.Authenticators;
using SyncApp.Helper;
using SyncApp.Interfaces;
using SyncApp.Models;
using System.Reflection.Metadata;

namespace SyncApp.Services.TopDesk;

public class TopDeskClient(SystemConfig config, IRestClient httpClient, ILogger<TopDeskClient> logger) : ISystemClient
{
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

    public async Task<bool> ValidateFieldExistsAsync(string objectType, string fieldName)
    {
        // Implementation note: TopDesk doesn't have a simple metadata API to check fields for a specific Incident without fetching one.
        logger.LogInformation("Validating field {fieldName} on TopDesk object {objectType}.", fieldName, objectType);
        return await Task.FromResult(true);
    }

    public async Task<IEnumerable<SyncItem>> GetChangesAsync(string objectType)
    {
        try
        {
            logger.LogInformation("Fetching {objectType} from TopDesk API", objectType);
            var request = CreateBaseRequest($"/tas/api/{objectType}s");

            if (request is null)
            {
                logger.LogError("Error fetching changes from TopDesk: Request could not be created. {req}", request);
                return [];
            }

            request.AddQueryParameter("sort", "modificationDate:desc");
            request.AddQueryParameter("query", "category.name==Zesa;processingStatus.id==160932da-84fb-5bb0-942a-e6be6e1f20e1");

            var response = await httpClient.ExecuteAsync<IEnumerable<Incident>>(request);

            if (response.IsSuccessStatusCode
                && response.Data is not null)
            {
                var syncItems = new List<SyncItem>();
                foreach (var incident in response.Data)
                {
                    var syncItem = MapIncidentToSyncItem(incident);
                    var requests = await GetRequestsAsync(incident.Id);
                    var operators = await GetOperatorAsync(incident.Operator.Id);
                    var progressTrail = await GetProgressTrail(incident.Id);

                    syncItem.Fields["report"] = FormatRequests(requests);
                    
                    syncItems.Add(syncItem);
                }
                return syncItems;
            }
            return [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching changes from TopDesk");
        }

        return [];
    }

    public async Task<SyncItem?> GetItemByExternalIdAsync(string objectType, string externalIdField, string externalIdValue)
    {
        // In TopDesk, searching by custom field often requires a specific query syntax
        // GET /tas/api/incidents?query=externalIdField==externalIdValue
        logger.LogInformation("Searching TopDesk {objectType} for {externalIdField} = {externalIdValue}", objectType, externalIdField, externalIdValue);
        return await Task.FromResult<SyncItem?>(null);
    }

    public async Task<string> CreateItemAsync(string objectType, SyncItem item, string externalIdField, string externalIdValue)
    {
        logger.LogInformation("Creating TopDesk {objectType}", objectType);
        // POST /tas/api/incidents
        // Body: item.Fields + { externalIdField: externalIdValue }
        return await Task.FromResult("NEW_ID");
    }

    public async Task UpdateItemAsync(string objectType, string id, SyncItem item)
    {
        logger.LogInformation("Updating TopDesk {objectType} {id}", objectType, id);
        // PUT /tas/api/incidents/{id}
    }

    private static SyncItem MapIncidentToSyncItem(Incident incident)
    {
        var fields = new Dictionary<string, object>();

        // Primitive Felder
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

        // Verknuepfte Objekte (nur ID + Name)
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

        // Optional Fields (nach Bedarf)
        AddOptionalFields(fields, incident.OptionalFields1, "optionalFields1");
        AddOptionalFields(fields, incident.OptionalFields2, "optionalFields2");

        return new SyncItem
        {
            Id = incident.Id,
            Fields = fields
        };
    }

    private static void AddOptionalFields(Dictionary<string, object> dict, OptionalFields? fields, string prefix)
    {
        if (fields is null) return;

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

    private async Task<IEnumerable<TopDeskRequest>> GetRequestsAsync(string incidentId)
    {
        try
        {
            var request = CreateBaseRequest($"/tas/api/incidents/id/{incidentId}/requests");
            if (request is null) return [];

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

    private async Task<OperatorFull?> GetOperatorAsync(string pOperatorId)
    {
        try
        {
            var request = CreateBaseRequest($"/tas/api/operators/id/{pOperatorId}");
            if (request is null) return null;

            var response = await httpClient.ExecuteAsync<OperatorFull>(request);
            if (response.IsSuccessStatusCode && response.Data is not null)
            {
                return response.Data;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching operator {incidentId}", pOperatorId);
        }
        return null;
    }

    private async Task<IEnumerable<TopDeskProgressTrailItem>> GetProgressTrail(string pIncidentId) 
    {
        try
        {
            var request = CreateBaseRequest($"tas/api/incidents/id/{pIncidentId}/progresstrail");
            if (request is null) return [];

            var response = await httpClient.ExecuteAsync<IEnumerable<TopDeskProgressTrailItem>>(request);
            if (response.IsSuccessStatusCode && response.Data is not null)
            {
                return response.Data;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching progress trail for {incidentId}", pIncidentId);
        }
        return [];
    }

    private static string FormatRequests(IEnumerable<TopDeskRequest> requests)
    {
        if (requests is null || !requests.Any()) return string.Empty;

        var sb = new System.Text.StringBuilder();
        foreach (var req in requests.OrderBy(r => r.EntryDate))
        {
            sb.AppendLine($"<b>{req.EntryDate:yyyy-MM-dd HH:mm} - {req.Operator?.Name ?? "Unknown"}</b><br>");
            sb.AppendLine(req.MemoText?.Replace("\n", "<br>") ?? "");
            sb.AppendLine("<br><hr><br>");
        }
        return sb.ToString();
    }
}
