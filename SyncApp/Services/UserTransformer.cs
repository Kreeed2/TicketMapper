using SyncApp.Interfaces;
using SyncApp.Models;
using System.Text.RegularExpressions;

namespace SyncApp.Services;

public class UserTransformer(FieldMapping pConfig, ILogger<UserTransformer> pLogger) : ITransformer
{
    public string? Transform(string? value, FieldMappingTransform transformType)
    {
        if (string.IsNullOrEmpty(value)) return null;

        return transformType switch
        {
            FieldMappingTransform.Pattern => ApplyPattern(value),
            FieldMappingTransform.Lookup => value,
            _ => value
        };
    }

    private string ApplyPattern(string sourceUser)
    {
        if (string.IsNullOrEmpty(pConfig.Pattern))
        {
            pLogger.LogWarning("UserMapping strategy is 'pattern' but no pattern is defined.");
            return sourceUser;
        }

        try
        {
            if (pConfig.Pattern.Contains("{0}"))
            {
                return string.Format(pConfig.Pattern, sourceUser);
            }

            return sourceUser;
        }
        catch (Exception ex)
        {
            pLogger.LogError(ex, "Error applying user mapping pattern '{pattern}' to user '{user}'", pConfig.Pattern, sourceUser);
            return sourceUser;
        }
    }
}
