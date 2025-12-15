using SyncApp.Interfaces;
using SyncApp.Models;
using System.Text.RegularExpressions;

namespace SyncApp.Services;

public class UserMappingService(AppConfiguration config, ILogger<UserMappingService> logger) : IUserMappingService
{
    public string MapUser(string sourceUser)
    {
        if (string.IsNullOrWhiteSpace(sourceUser)) return sourceUser;

        // 1. Manual Mapping
        var manualMapping = config.UserMapping.ManualMappings
            .FirstOrDefault(m => m.Source.Equals(sourceUser, StringComparison.OrdinalIgnoreCase));

        if (manualMapping != null)
        {
            logger.LogDebug("User '{source}' mapped manually to '{target}'.", sourceUser, manualMapping.Target);
            return manualMapping.Target;
        }

        // 2. Strategy
        return config.UserMapping.DefaultStrategy.ToLower() switch
        {
            "pattern" => ApplyPattern(sourceUser),
            "email_lookup" => sourceUser, // TODO: Implement if we have access to source system user list
            _ => sourceUser
        };
    }

    private string ApplyPattern(string sourceUser)
    {
        if (string.IsNullOrEmpty(config.UserMapping.Pattern))
        {
            logger.LogWarning("UserMapping strategy is 'pattern' but no pattern is defined.");
            return sourceUser;
        }

        try
        {
            // Simple string format if it contains {0}
            if (config.UserMapping.Pattern.Contains("{0}"))
            {
                return string.Format(config.UserMapping.Pattern, sourceUser);
            }

            // Regex replacement if pattern looks like regex? 
            // For now, let's stick to simple formatting as requested: "{0}@newdomain.com"
            return sourceUser;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error applying user mapping pattern '{pattern}' to user '{user}'", config.UserMapping.Pattern, sourceUser);
            return sourceUser;
        }
    }
}
