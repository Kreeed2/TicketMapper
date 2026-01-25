using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RestSharp;
using SyncApp.Interfaces;
using SyncApp.Models;
using SyncApp.Services.Devops;
using SyncApp.Services.TopDesk;
using System.Text.RegularExpressions;

namespace SyncApp.Services;

public class UserTransformer(IServiceProvider pServiceProvider, ILogger<UserTransformer> pLogger) : ITransformer
{
    private FieldMapping? mConfig;
    private IUserService? mSourceUserService;
    private IUserService? mTargetUserService;

    public ITransformer Configure(SystemMappingType pSourceSystem, SystemMappingType pTargetSystem, FieldMapping pMapping)
    {
        mConfig = pMapping;
        mSourceUserService = pSourceSystem switch
        {
            SystemMappingType.TopDesk => null,
            SystemMappingType.AzureDevOps => pServiceProvider.GetService<AzureDevOpsUserService>(),
            _ => throw new NotImplementedException()
        };

        mTargetUserService = pTargetSystem switch
        {
            SystemMappingType.TopDesk => null,
            SystemMappingType.AzureDevOps => pServiceProvider.GetService<AzureDevOpsUserService>(),
            _ => throw new NotImplementedException()
        };
        
        return this;
    }

    public async Task<string?> Transform(string? value, FieldMappingTransform transformType)
    {
        if (string.IsNullOrEmpty(value)) return null;

        return transformType switch
        {
            FieldMappingTransform.Pattern => ApplyPattern(value),
            FieldMappingTransform.Lookup => await LookupUser(value),
            _ => value
        };
    }

    private string ApplyPattern(string sourceUser)
    {
        if (string.IsNullOrEmpty(mConfig?.Pattern))
        {
            pLogger.LogWarning("UserMapping strategy is 'pattern' but no pattern is defined.");
            return sourceUser;
        }

        try
        {
            if (mConfig.Pattern.Contains("{0}"))
            {
                return string.Format(mConfig.Pattern, sourceUser);
            }

            return sourceUser;
        }
        catch (Exception ex)
        {
            pLogger.LogError(ex, "Error applying user mapping pattern '{pattern}' to user '{user}'", mConfig.Pattern, sourceUser);
            return sourceUser;
        }
    }

    private async Task<string> LookupUser(string pSourceUser)
    {
        if (mTargetUserService is null)
        {
            pLogger.LogError("IUserService has not been configured. Please call Configure beforehand.");
            throw new InvalidOperationException("IUserService has not been configured. Please call Configure beforehand.");
        }

        var userObj = await mTargetUserService.LookupUserByDisplayNameAsync(pSourceUser);
        if (userObj is null)
        {
            pLogger.LogWarning("User '{user}' could not be found in target system during lookup.", pSourceUser);
            return string.Empty;
        }

        var userName = userObj.Fields.GetValueOrDefault("account")?.ToString() ?? string.Empty;
        var domain = userObj.Fields.GetValueOrDefault("domain")?.ToString() ?? string.Empty;

        return $"{domain}\\{userName}";
    }
}
