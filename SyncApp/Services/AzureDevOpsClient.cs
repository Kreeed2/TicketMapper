using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using SyncApp.Interfaces;
using SyncApp.Models;
using static Microsoft.VisualStudio.Services.Graph.Constants;

namespace SyncApp.Services;

public class AzureDevOpsClient(SystemConfig pConfig, WorkItemTrackingHttpClient pWitClient, ILogger<AzureDevOpsClient> pLogger) : ISystemClient
{
    public string SystemName => "azure_devops";

    public async Task<bool> ValidateFieldExistsAsync(string pObjectType, string pFieldName)
    {
        pLogger.LogInformation("Validating field {fieldName} on ADO WorkItem {objectType}.", pFieldName, pObjectType);
        if (!pConfig.Defaults.TryGetValue("project", out var project))
        {
            pLogger.LogError("Azure DevOps SystemConfig is missing the required 'project' setting.");
            return false;
        }

        try
        {
            // Versucht, die Feldbeschreibung abzurufen.
            // Wenn das Feld nicht existiert, wird eine Ausnahme ausgel�st ( typically VssServiceException).
            var field = await pWitClient.GetWorkItemTypeFieldAsync(
                project: project,
                type: pObjectType, // z.B. "Bug"
                field: pFieldName // z.B. "Custom.TopDeskId"
            );

            // Wenn der Aufruf erfolgreich ist, existiert das Feld
            pLogger.LogInformation("Field '{objectType}' successfully validated on '{fieldName}' in project '{project}'.", pObjectType, pFieldName, project);
            return true;
        }
        catch (Exception ex)
        {
            // Nur wenn das Feld nicht gefunden wird
            // Eine VssServiceException (mit HTTP 404) w�rde hier landen.
            pLogger.LogError(ex, "Field '{objectType}' successfully validated on '{fieldName}' in project '{project}'.", pObjectType, pFieldName, project);
            return false;
        }
    }

    public async Task<IEnumerable<SyncItem>> GetChangesAsync(string objectType)
    {
        // WIQL query to get recent items
        pLogger.LogInformation("Fetching changes from ADO for {objectType}", objectType);
        pLogger.LogWarning("Real ADO API implementation incomplete. Returning empty list.");
        return await Task.FromResult(new List<SyncItem>());
    }

    public async Task<SyncItem?> GetItemByExternalIdAsync(string objectType, string externalIdField, string externalIdValue)
    {
        pLogger.LogInformation("Searching ADO {objectType} for {externalIdField} = {externalIdValue}", objectType, externalIdField, externalIdValue);
        // WIQL Query: Select [System.Id] From WorkItems Where [WorkItemType] = '{objectType}' AND [{externalIdField}] = '{externalIdValue}'
        return await Task.FromResult<SyncItem?>(null);
    }

    public async Task<string> CreateItemAsync(string pObjectType, SyncItem pItem, string pExternalIdField, string pExternalIdValue)
    {
        pLogger.LogInformation("Creating ADO {objectType} with external ID {externalIdValue}", pObjectType, pExternalIdValue);

        if (!pConfig.Defaults.TryGetValue("project", out var project))
        {
            pLogger.LogError("Azure DevOps SystemConfig is missing the required 'project' setting.");
            throw new InvalidOperationException("Project not configured for ADO client.");
        }

        try
        {
            // 1. Erstellen des JSON Patch Dokuments
            var patchDocument = new JsonPatchDocument();

            // 2. Fuegen Sie alle gemappten Felder aus dem SyncItem hinzu
            foreach (var field in pItem.Fields)
            {
                // Alle Felder werden als 'add'-Operationen hinzugefuegt
                patchDocument.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = $"/fields/{field.Key}",
                    Value = field.Value
                });
            }

            // 3. Fuegen Sie das externe ID-Feld hinzu, das die ID des Source-Systems speichert
            patchDocument.Add(new JsonPatchOperation()
            {
                Operation = Operation.Add,
                Path = $"/fields/{pExternalIdField}",
                Value = pExternalIdValue
            });

            // 4. API-Aufruf zur Erstellung des Work Items
            var newWorkItem = await pWitClient.CreateWorkItemAsync(
                document: patchDocument,
                project: project,
                type: pObjectType,
                validateOnly: false, // Auf 'true' setzen, um nur die Validierung durchzuf�hren
                bypassRules: false // Auf 'true' setzen, um Regeln zu umgehen (nur f�r Admins)
            );

            if (newWorkItem.Id.HasValue)
            {
                pLogger.LogInformation("Successfully created ADO {objectType} ID: {newWorkItem.Id.Value} in project {project}.", pObjectType, newWorkItem.Id.Value, project);
                return newWorkItem.Id.Value.ToString();
            }

            pLogger.LogError("ADO API call was successful but new Work Item ID is missing.");
            return string.Empty;
        }
        catch (Exception ex)
        {
            pLogger.LogError(ex, "Error creating ADO {objectType} in project {project}.", pObjectType, project);
            throw; // Werfen Sie die Ausnahme, damit der Worker den Fehler protokollieren kann
        }
    }

    public async Task UpdateItemAsync(string objectType, string id, SyncItem item)
    {
        pLogger.LogInformation("Updating ADO {objectType} {id}", objectType, id);

        if (!int.TryParse(id, out var workItemId))
        {
            pLogger.LogError("Invalid ADO Work Item ID: {id}", id);
            throw new ArgumentException($"Invalid ADO Work Item ID: {id}", nameof(id));
        }

        if (!pConfig.Defaults.TryGetValue("project", out var project))
        {
            pLogger.LogError("Azure DevOps SystemConfig is missing the required 'project' setting.");
            throw new InvalidOperationException("Project not configured for ADO client.");
        }

        try
        {
            var patchDocument = new JsonPatchDocument();

            foreach (var field in item.Fields)
            {
                patchDocument.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = $"/fields/{field.Key}",
                    Value = field.Value
                });
            }

            await pWitClient.UpdateWorkItemAsync(
                document: patchDocument,
                id: workItemId,
                project: project,
                validateOnly: false,
                bypassRules: false
            );

            pLogger.LogInformation("Successfully updated ADO {objectType} {id}", objectType, id);
        }
        catch (Exception ex)
        {
            pLogger.LogError(ex, "Error updating ADO {objectType} {id}", objectType, id);
            throw;
        }
    }
}
