using RestSharp;
using RestSharp.Authenticators;
using SyncApp.Helper;
using SyncApp.Interfaces;
using SyncApp.Models;
using SyncApp.Models.AzureDevOps;
using SyncApp.Services.TopDesk;

namespace SyncApp.Services.Devops;

public class AzureDevOpsUserService(SystemConfig config, IRestClient httpClient, ILogger logger) : IUserService
{
    private RestRequest? CreateBaseRequest(string pUrlPath)
    {
        if (Uri.TryCreate(config.Url, UriKind.Absolute, out var baseUrl)
            && !string.IsNullOrEmpty(config.Token))
        {
            var request = new RestRequest($"{baseUrl}{pUrlPath}")
            {
                Authenticator = new HttpBasicAuthenticator(string.Empty, config.Token)
            };

            return request;
        }
        return null;
    }

    async Task<SyncItem?> IUserService.LookupUserByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogInformation("No email given. User-Lookup stopped.");
            return null;
        }

        try
        {
            var syncItem = new SyncItem() { Id = Guid.Empty.ToString() };

            logger.LogInformation("Fetching Azure DevOps user by email: {email}", email);
            var request = CreateBaseRequest($"_apis/identities");

            if (request is null)
            {
                logger.LogError("Error fetching changes from TopDesk: Request could not be created. {req}", request);
                return null;
            }

            request.AddQueryParameter("searchFilter", "General");
            request.AddQueryParameter("api-version", "7.0");
            request.AddQueryParameter("queryMembership", "None");
            request.AddQueryParameter("filterValue", email);

            var response = await httpClient.ExecuteAsync<AzureDevopsIdentityRequest>(request);

            if (response.IsSuccessStatusCode
                && response.Data?.Count > 0)
            {
                logger.LogInformation("Found Azure DevOps user '{email}': {principalName}", email, response.Data.Identities.First().ProviderDisplayName);

                syncItem.Fields.Add("PrincipalName", response.Data.Identities.First().ProviderDisplayName);
                return syncItem;
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

    async Task<SyncItem?> IUserService.LookupUserByDisplayNameAsync(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            logger.LogInformation("No user name given. User-Lookup stopped.");
            return null;
        }

        try
        {
            var syncItem = new SyncItem() { Id = Guid.Empty.ToString() };

            logger.LogInformation("Fetching Azure DevOps user by name: {name}", displayName);
            var request = CreateBaseRequest($"_apis/identities");

            if (request is null)
            {
                logger.LogError("Error fetching changes from TopDesk: Request could not be created. {req}", request);
                return null;
            }

            request.AddQueryParameter("searchFilter", "General");
            request.AddQueryParameter("api-version", "7.0");
            request.AddQueryParameter("queryMembership", "None");
            request.AddQueryParameter("filterValue", displayName);

            var response = await httpClient.ExecuteAsync<AzureDevopsIdentityRequest>(request);

            if (response.IsSuccessStatusCode
                && response.Data?.Count > 0)
            {
                logger.LogInformation("Found Azure DevOps user.");

                return ToSyncItem(response.Data.Identities[0]);
            }

            logger.LogWarning("No Azure DevOps user found with name: {name}", displayName);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error looking up user by name in Azure DevOps: {name}", displayName);
            return null;
        }
    }

    public Task<SyncItem?> LookupUserBySystemIdAsync(string systemId)
    {
        throw new NotImplementedException();
    }

    private static SyncItem ToSyncItem(AzureDevopsIdentity identity)
    {
        var fields = new Dictionary<string, object>();

        Utilities.AddIfNotNull(fields, "descriptor", identity.Descriptor);
        Utilities.AddIfNotNull(fields, "subjectDescriptor", identity.SubjectDescriptor);
        Utilities.AddIfNotNull(fields, "providerDisplayName", identity.ProviderDisplayName);
        Utilities.AddIfNotNull(fields, "isActive", identity.IsActive);
        Utilities.AddIfNotNull(fields, "resourceVersion", identity.ResourceVersion);
        Utilities.AddIfNotNull(fields, "metaTypeId", identity.MetaTypeId);

        Utilities.AddIfNotNull(fields, "schemaClassName", identity.Properties.SchemaClassName.Value);

        Utilities.AddIfNotNull(fields, "description", identity.Properties.Description.Value);

        Utilities.AddIfNotNull(fields, "domain", identity.Properties.Domain.Value);

        Utilities.AddIfNotNull(fields, "account", identity.Properties.Account.Value);

        Utilities.AddIfNotNull(fields, "DN", identity.Properties.DN.Value);

        Utilities.AddIfNotNull(fields, "mail", identity.Properties.Mail.Value);

        Utilities.AddIfNotNull(fields, "specialType", identity.Properties.SpecialType.Value);

        Utilities.AddIfNotNull(fields, "complianceValidated", identity.Properties.ComplianceValidated.Value);

        Utilities.AddIfNotNull(fields, "directoryAlias", identity.Properties.DirectoryAlias.Value);

        var syncItem = new SyncItem()
        {
            Id = identity.Id,
            Fields = fields
        };

        return syncItem;
    }
}