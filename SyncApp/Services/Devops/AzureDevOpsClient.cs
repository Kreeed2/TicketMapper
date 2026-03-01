using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using SyncApp.Interfaces;
using SyncApp.Models;
using System.Text.RegularExpressions;

namespace SyncApp.Services.Devops;

public class AzureDevOpsClient(SystemConfig pConfig, WorkItemTrackingHttpClient pWitClient, ILogger<AzureDevOpsClient> pLogger) : ISystemClient
{
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

        if (!pConfig.Defaults.TryGetValue("project", out var project))
        {
            pLogger.LogError("Azure DevOps SystemConfig is missing the required 'project' setting.");
            return null;
        }

        var query = new Wiql() { Query = $"SELECT [ID] FROM WorkItem WHERE [System.WorkItemType] = '{objectType}' AND [{externalIdField}] = '{externalIdValue}' AND [System.TeamProject] = @project" };

        try
        {
            var workItemQueryResponse = await pWitClient.QueryByWiqlAsync(
                wiql: query,
                project: project
            );

            var ids = workItemQueryResponse?.WorkItems.Select(workItemReference => workItemReference.Id);

            if (ids is null || !ids.Any())
                return null;

            var workItems = await pWitClient.GetWorkItemsAsync(ids);
            return workItems.Select(wit => new SyncItem() { Id = wit.Id.ToString() ?? throw new InvalidOperationException("WorkItem has null id."), Fields = wit.Fields.ToDictionary() }).FirstOrDefault();
        }
        catch (Exception ex)
        {
            pLogger.LogError(ex, "Error fetching ADO {objectType} in project '{project}' with '{id}'.", objectType, project, externalIdValue);
            throw;
        }
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


            if (item.ForeignFields != null && item.ForeignFields.Count > 0)
            {
                await ProcessForeignFieldsAsync(project, workItemId, item.ForeignFields);
            }

            pLogger.LogInformation("Successfully updated ADO {objectType} {id}", objectType, id);
        }
        catch (Exception ex)
        {
            pLogger.LogError(ex, "Error updating ADO {objectType} {id}", objectType, id);
            throw;
        }
    }
    private async Task ProcessForeignFieldsAsync(string pProject, int pWorkItemId, Dictionary<string, IEnumerable<SyncItem>> pForeignFields)
    {
        foreach (var foreignField in pForeignFields)
        {
            if (foreignField.Key.Equals("comments", StringComparison.OrdinalIgnoreCase))
            {
                await CreateCommentsAsync(pProject, pWorkItemId, foreignField.Value);
            }
            else if (foreignField.Key.Equals("attachments", StringComparison.OrdinalIgnoreCase))
            {
                await CreateAttachmentsAsync(pProject, pWorkItemId, foreignField.Value);
            }
        }
    }

    private async Task CreateCommentsAsync(string pProject, int pWorkItemId, IEnumerable<SyncItem> pComments)
    {
        var existingCommentsResponse = await pWitClient.GetCommentsAsync(pProject, pWorkItemId);
        var existingComments = existingCommentsResponse?.Comments ?? [];

        foreach (var commentSyncItem in pComments)
        {
            var commentText = GetCommentText(commentSyncItem);

            if (!string.IsNullOrWhiteSpace(commentText))
            {
                // Uniquer Marker, um den Kommentar wiederzufinden
                var uniqueMarker = $"[SYNC:{commentSyncItem.Id}]";
                // Ersteller hinzufügen
                commentSyncItem.Fields.TryGetValue("operator.name", out var commentOperator);
                // Wir hängen den Marker an den Text an
                var fullCommentText = $"{commentOperator ?? "Kein Ersteller gefunden"}<br><br>{commentText}<br><br>{uniqueMarker}";

                // Prüfen, ob der Kommentar schon existiert (anhand des Markers)
                var existingComment = existingComments.FirstOrDefault(c => c.Text != null && c.Text.Contains(uniqueMarker));

                if (existingComment != null)
                {
                    // Update nur wenn sich der Text geändert hat
                    if (!AreCommentTextsEqual(existingComment.Text, fullCommentText))
                    {
                        var commentUpdate = new CommentUpdate() { Text = fullCommentText };
                        await pWitClient.UpdateCommentAsync(
                            request: commentUpdate,
                            project: pProject,
                            workItemId: pWorkItemId,
                            commentId: existingComment.Id
                        );
                        pLogger.LogInformation("Updated comment {commentId} on work item {workItemId}.", existingComment.Id, pWorkItemId);
                    }
                }
                else
                {
                    // Neu erstellen
                    var commentCreate = new CommentCreate() { Text = fullCommentText };
                    await pWitClient.AddCommentAsync(
                       request: commentCreate,
                       project: pProject,
                       workItemId: pWorkItemId
                    );
                    pLogger.LogInformation("Created new comment on work item {workItemId}.", pWorkItemId);
                }
            }
        }
    }

    private async Task CreateAttachmentsAsync(string pProject, int pWorkItemId, IEnumerable<SyncItem> pAttachments)
    {
        // Bestehende Work Item Details abrufen, um Duplikate zu vermeiden
        var workItem = await pWitClient.GetWorkItemAsync(pWorkItemId, expand: WorkItemExpand.Relations);
        var existingFiles = workItem.Relations?
            .Where(r => r.Rel == "AttachedFile")
            .Select(r => r.Attributes["name"]?.ToString())
            .ToList() ?? [];

        foreach (var attachmentItem in pAttachments)
        {
            if (!attachmentItem.Fields.TryGetValue("fileName", out var nameObj) ||
                !attachmentItem.Fields.TryGetValue("content", out var contentObj)) continue;

            string fileName = nameObj.ToString()!;
            byte[] content = (byte[])contentObj;

            // Prüfen, ob Datei bereits angehängt ist (einfacher Namensvergleich)
            if (existingFiles.Contains(fileName)) continue;

            using var stream = new MemoryStream(content);

            // 1. Datei zu ADO hochladen
            var attachmentRef = await pWitClient.CreateAttachmentAsync(stream, fileName: fileName, project: pProject);

            // 2. Verknüpfung am Work Item erstellen
            var patchDocument = new JsonPatchDocument
            {
                new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = "/relations/-",
                    Value = new
                    {
                        rel = "AttachedFile",
                        url = attachmentRef.Url,
                        attributes = new { comment = $"Synced from TopDesk: {attachmentItem.Id}" }
                    }
                }
            };

            await pWitClient.UpdateWorkItemAsync(patchDocument, pWorkItemId);
            pLogger.LogInformation("Attachment {fileName} added to Work Item {pWorkItemId}.", fileName, pWorkItemId);
        }
    }

    private static bool AreCommentTextsEqual(string pText1, string pText2)
    {
        return NormalizeCommentText(pText1).Equals(NormalizeCommentText(pText2), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeCommentText(string pText)
    {
        if (string.IsNullOrEmpty(pText)) return string.Empty;
        // Entferne Whitespace um Formatierungsunterschiede zu ignorieren
        return Regex.Replace(pText, @"\s+", "");
    }

    private static string GetCommentText(SyncItem pCommentItem)
    {
        // Flexible field lookup for comment text
        string[] candidates = ["memoText", "text", "body", "content"];

        foreach (var key in candidates)
        {
            if (pCommentItem.Fields.TryGetValue(key, out var val) && val != null)
            {
                var str = val.ToString();
                if (!string.IsNullOrWhiteSpace(str)) return str;
            }
        }

        // Final fallback: Check case-insensitive keys
        foreach (var key in candidates)
        {
            var match = pCommentItem.Fields.Keys.FirstOrDefault(k => k.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (match != null && pCommentItem.Fields[match] != null)
            {
                var str = pCommentItem.Fields[match].ToString();
                if (!string.IsNullOrWhiteSpace(str)) return str;
            }
        }

        return string.Empty;
    }
}
