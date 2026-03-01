using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SyncApp.Interfaces;
using SyncApp.Models;
using SyncApp.Services.Devops;

namespace SyncApp.Services;

public class AzureDevOpsItemSearchTransformer(IServiceProvider pServiceProvider, ILogger<AzureDevOpsItemSearchTransformer> pLogger) : ITransformer
{
    private FieldMapping? mConfig;
    private ISystemClient? mAzureDevOpsClient;

    public ITransformer Configure(SystemMappingType pSourceSystem, SystemMappingType pTargetSystem, FieldMapping pMapping)
    {
        mConfig = pMapping;

        // The query runs against Azure DevOps, so we always need the ADO client.
        mAzureDevOpsClient = pServiceProvider.GetRequiredService<AzureDevOpsClient>();

        return this;
    }

    public async Task<string?> Transform(string? value, FieldMappingTransform transformType)
    {
        if (string.IsNullOrEmpty(value) || transformType != FieldMappingTransform.ItemSearch || mConfig is null)
            return null;

        if (mAzureDevOpsClient is null)
        {
            pLogger.LogError("ISystemClient has not been configured. Please call Configure beforehand.");
            throw new InvalidOperationException("ISystemClient has not been configured. Please call Configure beforehand.");
        }

        if (string.IsNullOrEmpty(mConfig.SearchItemType))
        {
            pLogger.LogError("SearchItemType is missing in mapping configuration. It is required for ItemSearch transform.");
            return null;
        }

        if (string.IsNullOrEmpty(mConfig.SearchField))
        {
            pLogger.LogError("SearchField is missing in mapping configuration. It is required for ItemSearch transform.");
            return null;
        }

        try
        {
            // Execute the Azure DevOps query
            var result = await mAzureDevOpsClient.GetItemByExternalIdAsync(
                mConfig.SearchItemType,
                mConfig.SearchField,
                value
            );

            if (result != null)
            {
                pLogger.LogInformation("ItemSearch found ADO {ObjectType} matching {SearchField}={Value} mapping to ID {Id}", mConfig.SearchItemType, mConfig.SearchField, value, result.Id);

                // For link types we must use the special url provided by the relations, 
                // However, since it's just the item ID right now, we can just return the item's organization reference url or ID
                string urlStr = result.Fields.GetValueOrDefault("url")?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(urlStr)) return urlStr;

                // Return just the ID otherwise (should generally not happen since url usually exists)
                return result.Id;
            }

            pLogger.LogWarning("ItemSearch did not find any ADO {ObjectType} matching {SearchField}={Value}", mConfig.SearchItemType, mConfig.SearchField, value);
        }
        catch (Exception ex)
        {
            pLogger.LogError(ex, "Error occurred during ItemSearch transform for ADO {ObjectType} matching {SearchField}={Value}", mConfig.SearchItemType, mConfig.SearchField, value);
        }

        return null;
    }
}
