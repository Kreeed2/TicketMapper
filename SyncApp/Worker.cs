using SyncApp.Interfaces;
using SyncApp.Models;
using SyncApp.Services;

namespace SyncApp;

public class Worker(ILogger<Worker> logger, ClientFactory clientFactory, ITransformer transformer, AppConfiguration config, bool dryRun) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SyncApp started. DryRun: {DryRun}", dryRun);

        // 1. Validation Phase
        if (!await ValidateConfigurationAsync())
        {
            logger.LogCritical("Configuration validation failed. Stopping.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Starting sync cycle at: {time}", DateTimeOffset.Now);

                foreach (var mapping in config.Mappings)
                {
                    await ProcessMappingAsync(mapping, stoppingToken);
                }

                logger.LogInformation("Sync cycle completed. Sleeping for {seconds} seconds.", config.Settings.PollIntervalSeconds);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during the sync cycle.");
            }

            await Task.Delay(config.Settings.PollIntervalSeconds * 1000, stoppingToken);
        }
    }

    private async Task<bool> ValidateConfigurationAsync()
    {
        foreach (var mapping in config.Mappings)
        {
            if (!config.Systems.TryGetValue(mapping.TargetSystem, out var targetSysConfig))
            {
                logger.LogError($"Target system '{mapping.TargetSystem}' in mapping '{mapping.Name}' is not defined in systems.");
                return false;
            }

            // Check external_id_field on target
            var targetClient = clientFactory.CreateClient(targetSysConfig);
            if (!await targetClient.ValidateFieldExistsAsync(mapping.TargetObject, mapping.ExternalIdField))
            {
                logger.LogError($"External ID field '{mapping.ExternalIdField}' does not exist on target system '{mapping.TargetSystem}' object '{mapping.TargetObject}'.");
                return false;
            }
        }
        return true;
    }

    private async Task ProcessMappingAsync(MappingConfig mapping, CancellationToken stoppingToken)
    {
        logger.LogInformation("Processing mapping: {name}", mapping.Name);

        if (!config.Systems.TryGetValue(mapping.SourceSystem, out var sourceSysConfig) ||
            !config.Systems.TryGetValue(mapping.TargetSystem, out var targetSysConfig))
        {
            logger.LogError("Invalid system keys in mapping {name}", mapping.Name);
            return;
        }

        var sourceClient = clientFactory.CreateClient(sourceSysConfig);
        var targetClient = clientFactory.CreateClient(targetSysConfig);

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
                    logger.LogInformation("Item {id} not found in target. Creating...", sourceItem.Id);
                    if (!dryRun)
                    {
                        await targetClient.CreateItemAsync(mapping.TargetObject, targetItemFields, mapping.ExternalIdField, sourceItem.Id);
                    }
                }
                else
                {
                    // Update
                    if (HasChanges(sourceItem, targetItem, mapping))
                    {
                         logger.LogInformation("Item {id} found in target. Updating...", sourceItem.Id);
                         if (!dryRun)
                         {
                             await targetClient.UpdateItemAsync(mapping.TargetObject, targetItem.Id, targetItemFields);
                         }
                    }
                    else
                    {
                        logger.LogDebug("Item {id} is up to date.", sourceItem.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing item {id} in mapping {name}", sourceItem.Id, mapping.Name);
            }
        }
    }

    private SyncItem MapFields(SyncItem source, MappingConfig mapping)
    {
        var target = new SyncItem() { Id = Guid.Empty.ToString() };

        foreach (var field in mapping.Fields)
        {
            if (source.Fields.TryGetValue(field.Source, out var value))
            {
                string stringValue = value?.ToString();
                string transformed = transformer.Transform(stringValue, field.Transform);
                target.Fields[field.Target] = transformed;
            }
        }
        return target;
    }

    private bool HasChanges(SyncItem source, SyncItem target, MappingConfig mapping)
    {
        // Simple comparison of mapped fields
        foreach (var field in mapping.Fields)
        {
            if (source.Fields.TryGetValue(field.Source, out var sourceValObj))
            {
                var sourceVal = transformer.Transform(sourceValObj?.ToString(), field.Transform);

                if (target.Fields.TryGetValue(field.Target, out var targetValObj))
                {
                    var targetVal = targetValObj?.ToString();
                    if (sourceVal != targetVal) return true;
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
