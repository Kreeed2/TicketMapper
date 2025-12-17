using Microsoft.VisualStudio.Services.Graph.Client;
using SyncApp.Interfaces;
using SyncApp.Models;

namespace SyncApp.Services.Devops;

public class AzureDevOpsUserService(GraphHttpClient graphClient, ILogger<AzureDevOpsUserService> logger) : IUserService
{
    public async Task<string?> LookupUserByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        try
        {
            logger.LogDebug("Looking up Azure DevOps user by email: {email}", email);
            
            var users = await graphClient.ListUsersAsync(
                subjectTypes: [email]
            );

            var user = users.GraphUsers.FirstOrDefault(u => 
                u.PrincipalName?.Equals(email, StringComparison.OrdinalIgnoreCase) == true);

            if (user != null)
            {
                logger.LogInformation("Found Azure DevOps user '{email}': {principalName}", email, user.PrincipalName);
                return user.PrincipalName;
            }

            logger.LogWarning("No Azure DevOps user found with email: {email}", email);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error looking up user by email in Azure DevOps: {email}", email);
            return null;
        }
    }

    public async Task<string?> LookupUserByDisplayNameAsync(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return null;
        }

        try
        {
            logger.LogDebug("Looking up Azure DevOps user by display name: {displayName}", displayName);
            
            var users = await graphClient.ListUsersAsync(
                subjectTypes: [ displayName ]
            );

            var user = users.GraphUsers.FirstOrDefault(u => 
                u.DisplayName?.Equals(displayName, StringComparison.OrdinalIgnoreCase) == true);

            if (user != null)
            {
                logger.LogInformation("Found Azure DevOps user '{displayName}': {principalName}", displayName, user.PrincipalName);
                return user.PrincipalName;
            }

            logger.LogWarning("No Azure DevOps user found with display name: {displayName}", displayName);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error looking up user by display name in Azure DevOps: {displayName}", displayName);
            return null;
        }
    }

    Task<SyncItem?> IUserService.LookupUserByEmailAsync(string email)
    {
        throw new NotImplementedException();
    }

    Task<SyncItem?> IUserService.LookupUserByDisplayNameAsync(string displayName)
    {
        throw new NotImplementedException();
    }

    public Task<SyncItem?> LookupUserBySystemIdAsync(string systemId)
    {
        throw new NotImplementedException();
    }
}