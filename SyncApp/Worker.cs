using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SyncApp.Interfaces;
using SyncApp.Models;
using SyncApp.Services;

namespace SyncApp
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ClientFactory _clientFactory;
        private readonly ITransformer _transformer;
        private readonly AppConfiguration _config;
        private readonly bool _dryRun;

        public Worker(ILogger<Worker> logger, ClientFactory clientFactory, ITransformer transformer, AppConfiguration config, bool dryRun)
        {
            _logger = logger;
            _clientFactory = clientFactory;
            _transformer = transformer;
            _config = config;
            _dryRun = dryRun;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SyncApp started. DryRun: {DryRun}", _dryRun);

            // 1. Validation Phase
            if (!await ValidateConfigurationAsync())
            {
                _logger.LogCritical("Configuration validation failed. Stopping.");
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting sync cycle at: {time}", DateTimeOffset.Now);

                    foreach (var mapping in _config.Mappings)
                    {
                        await ProcessMappingAsync(mapping, stoppingToken);
                    }

                    _logger.LogInformation("Sync cycle completed. Sleeping for {seconds} seconds.", _config.Settings.PollIntervalSeconds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred during the sync cycle.");
                }

                await Task.Delay(_config.Settings.PollIntervalSeconds * 1000, stoppingToken);
            }
        }

        private async Task<bool> ValidateConfigurationAsync()
        {
            foreach (var mapping in _config.Mappings)
            {
                if (!_config.Systems.TryGetValue(mapping.TargetSystem, out var targetSysConfig))
                {
                    _logger.LogError("Target system '{0}' in mapping '{1}' is not defined in systems.", mapping.TargetSystem, mapping.Name);
                    return false;
                }

                // Check external_id_field on target
                var targetClient = _clientFactory.CreateClient(targetSysConfig);
                if (!await targetClient.ValidateFieldExistsAsync(mapping.TargetObject, mapping.ExternalIdField))
                {
                    _logger.LogError("External ID field '{0}' does not exist on target system '{1}' object '{2}'.", mapping.ExternalIdField, mapping.TargetSystem, mapping.TargetObject);
                    return false;
                }
            }
            return true;
        }

        private async Task ProcessMappingAsync(MappingConfig mapping, CancellationToken stoppingToken)
        {
            _logger.LogInformation("Processing mapping: {name}", mapping.Name);

            if (!_config.Systems.TryGetValue(mapping.SourceSystem, out var sourceSysConfig) ||
                !_config.Systems.TryGetValue(mapping.TargetSystem, out var targetSysConfig))
            {
                _logger.LogError("Invalid system keys in mapping {name}", mapping.Name);
                return;
            }

            var sourceClient = _clientFactory.CreateClient(sourceSysConfig);
            var targetClient = _clientFactory.CreateClient(targetSysConfig);

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
                        _logger.LogInformation("Item {id} not found in target. Creating...", sourceItem.Id);
                        if (!_dryRun)
                        {
                            await targetClient.CreateItemAsync(mapping.TargetObject, targetItemFields, mapping.ExternalIdField, sourceItem.Id);
                        }
                    }
                    else
                    {
                        // Update
                        if (HasChanges(sourceItem, targetItem, mapping))
                        {
                             _logger.LogInformation("Item {id} found in target. Updating...", sourceItem.Id);
                             if (!_dryRun)
                             {
                                 await targetClient.UpdateItemAsync(mapping.TargetObject, targetItem.Id, targetItemFields);
                             }
                        }
                        else
                        {
                            _logger.LogDebug("Item {id} is up to date.", sourceItem.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing item {id} in mapping {name}", sourceItem.Id, mapping.Name);
                }
            }
        }

        private SyncItem MapFields(SyncItem source, MappingConfig mapping)
        {
            var target = new SyncItem();
            // We don't necessarily know the Target ID yet if it's a create, so we leave it null or fill it later.

            foreach (var field in mapping.Fields)
            {
                if (source.Fields.TryGetValue(field.Source, out var value))
                {
                    string stringValue = value?.ToString();
                    string transformed = _transformer.Transform(stringValue, field.Transform);
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
                    var sourceVal = _transformer.Transform(sourceValObj?.ToString(), field.Transform);

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
}
