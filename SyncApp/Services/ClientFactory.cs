using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using RestSharp;
using SyncApp.Interfaces;
using SyncApp.Models;

namespace SyncApp.Services;

public class ClientFactory(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
{
    public ISystemClient CreateClient(SystemConfig pConfig, bool pUseMock = false)
    {
        // If the URL contains "mock", we use the MockClient
        // We check for "mock" case-insensitively
        if (pConfig.Url.Contains("mock", StringComparison.OrdinalIgnoreCase))
        {
            return new MockClient(pConfig, loggerFactory.CreateLogger<MockClient>());
        }

        return pConfig.Type.ToLower() switch
        {
            "topdesk" => new TopDeskClient(pConfig, serviceProvider.GetRequiredService<IRestClient>(), loggerFactory.CreateLogger<TopDeskClient>()),
            "azure_devops" => CreateAzureDevOpsClient(pConfig),
            _ => new MockClient(pConfig, loggerFactory.CreateLogger<MockClient>()),// Fallback or throw
        };
    }

    private AzureDevOpsClient CreateAzureDevOpsClient(SystemConfig pConfig)
    {
        if (string.IsNullOrEmpty(pConfig.Url) || string.IsNullOrEmpty(pConfig.Token))
        {
            throw new InvalidOperationException($"Configuration for Azure DevOps system (URL: {pConfig.Url}) is missing URL or Token.");
        }

        //TODO: Personal access tokens are being deprecated
        var credentials = new VssBasicCredential(string.Empty, pConfig.Token); // Standard für PAT: Benutzername ist leer
        var connection = new VssConnection(new Uri(pConfig.Url), credentials);
        var witClient = connection.GetClient<WorkItemTrackingHttpClient>();
        return new AzureDevOpsClient(pConfig, witClient, loggerFactory.CreateLogger<AzureDevOpsClient>());
    }
}
