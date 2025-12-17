using SyncApp.Interfaces;
using SyncApp.Models;
using SyncApp.Services;

namespace SyncApp;

public class Worker(ILogger<Worker> pLogger, ClientFactory pClientFactory, TransformerFactory pTransformerFactory, AppConfiguration pConfig, bool pDryRun) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        pLogger.LogInformation("SyncApp started. DryRun: {DryRun}", pDryRun);

        // 1. Validation Phase
        if (!await ValidateConfigurationAsync())
        {
            pLogger.LogCritical("Configuration validation failed. Stopping.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                pLogger.LogInformation("Starting sync cycle at: {time}", DateTimeOffset.Now);

                foreach (var mapping in pConfig.Mappings)
                {
                    await ProcessMappingAsync(mapping, stoppingToken);
                }

                pLogger.LogInformation("Sync cycle completed. Sleeping for {seconds} seconds.", pConfig.Settings.PollIntervalSeconds);
            }
            catch (Exception ex)
            {
                pLogger.LogError(ex, "An error occurred during the sync cycle.");
            }

            await Task.Delay(pConfig.Settings.PollIntervalSeconds * 1000, stoppingToken);
        }
    }

    private async Task<bool> ValidateConfigurationAsync()
    {
        foreach (var mapping in pConfig.Mappings)
        {
            if (!pConfig.Systems.TryGetValue(mapping.TargetSystem, out var targetSysConfig))
            {
                pLogger.LogError("Target system '{targetSysConfig}' in mapping '{name}' is not defined in systems.", targetSysConfig, mapping.Name);
                return false;
            }

            // Check external_id_field on target
            var targetClient = pClientFactory.CreateClient(targetSysConfig);
            if (!await targetClient.ValidateFieldExistsAsync(mapping.TargetObject, mapping.ExternalIdField))
            {
                pLogger.LogError("External ID field '{externalIdField}' does not exist on target system '{targetSystem}' object '{targetObject}'.", mapping.ExternalIdField, mapping.TargetSystem, mapping.TargetObject);
                return false;
            }
        }
        return true;
    }

    private async Task ProcessMappingAsync(MappingConfig mapping, CancellationToken stoppingToken)
    {
        pLogger.LogInformation("Processing mapping: {name}", mapping.Name);

        if (!pConfig.Systems.TryGetValue(mapping.SourceSystem, out var sourceSysConfig) ||
            !pConfig.Systems.TryGetValue(mapping.TargetSystem, out var targetSysConfig))
        {
            pLogger.LogError("Invalid system keys in mapping {name}", mapping.Name);
            return;
        }

        var sourceClient = pClientFactory.CreateClient(sourceSysConfig);
        var targetClient = pClientFactory.CreateClient(targetSysConfig);

        // Fetch changes from Source
        var sourceItems = await sourceClient.GetChangesAsync(mapping.SourceObject);

        foreach (var sourceItem in sourceItems)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                // Check if exists in Target
                // We need to match based on the ExternalIdField in Target which should hold the SourceItem.Id
                var targetItem = await targetClient.GetItemByExternalIdAsync(mapping.TargetObject, mapping.ExternalIdField, sourceItem.Id);

                var targetItemFields = MapFields(sourceItem, mapping);

                if (targetItem == null)
                {
                    // Create
                    pLogger.LogInformation("Item {id} not found in target. Creating...", sourceItem.Id);
                    if (!pDryRun)
                    {
                        await targetClient.CreateItemAsync(mapping.TargetObject, targetItemFields, mapping.ExternalIdField, sourceItem.Id);
                    }
                }
                else
                {
                    // Update
                    if (HasChanges(sourceItem, targetItem, mapping))
                    {
                        pLogger.LogInformation("Item {id} found in target. Updating...", sourceItem.Id);
                        if (!pDryRun)
                        {
                            await targetClient.UpdateItemAsync(mapping.TargetObject, targetItem.Id, targetItemFields);
                        }
                    }
                    else
                    {
                        pLogger.LogDebug("Item {id} is up to date.", sourceItem.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                pLogger.LogError(ex, "Error processing item {id} in mapping {name}", sourceItem.Id, mapping.Name);
            }
        }
    }

    private SyncItem MapFields(SyncItem source, MappingConfig mapping)
    {
        var target = new SyncItem() { Id = Guid.Empty.ToString() };

        foreach (var field in mapping.Fields)
        {
            var transformer = pTransformerFactory.CreateTransformer(field);

            string? transformed = null;

            if (field.Transform == FieldMappingTransform.Static)
            {
                transformed = field.Source;
            }

            if (source.Fields.TryGetValue(field.Source, out var value))
            {
                string? stringValue = value?.ToString();
                transformed = transformer.Transform(stringValue, field.Transform);
            }

            if (transformed is not null)
            {
                target.Fields[field.Target] = transformed;
            }
        }
        return target;
    }

    private bool HasChanges(SyncItem source, SyncItem target, MappingConfig mapping)
    {
        // Simple comparison of mapped fields
        foreach (var field in mapping.Fields.Where(f => f.Update))
        {
            var transformer = pTransformerFactory.CreateTransformer(field);

            if (source.Fields.TryGetValue(field.Source, out var sourceValObj))
            {
                var sourceVal = transformer.Transform(sourceValObj?.ToString(), field.Transform);

                if (target.Fields.TryGetValue(field.Target, out var targetValObj))
                {
                    var targetVal = targetValObj?.ToString();
                    if (!sourceVal?.Equals(targetVal, StringComparison.OrdinalIgnoreCase) ?? true)
                    {
                        return true;
                    }
                }
                else if (!string.IsNullOrEmpty(sourceVal))
                {
                    // Target missing field but source has value
                    return true;
                }
            }
        }
        return false;
    }
}
