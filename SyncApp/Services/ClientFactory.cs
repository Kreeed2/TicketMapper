using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using RestSharp;
using SyncApp.Interfaces;
using SyncApp.Models;

namespace SyncApp.Services
{
    public class ClientFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILoggerFactory _loggerFactory;

        public ClientFactory(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        {
            _serviceProvider = serviceProvider;
            _loggerFactory = loggerFactory;
        }

        public ISystemClient CreateClient(SystemConfig config, bool useMock = false)
        {
            // If the URL contains "mock", we use the MockClient
            // We check for "mock" case-insensitively
            if (config.Url.Contains("mock", StringComparison.OrdinalIgnoreCase))
            {
                return new MockClient(config, _loggerFactory.CreateLogger<MockClient>());
            }

            return config.Type.ToLower() switch
            {
                "topdesk" => new TopDeskClient(config, new RestClient(), _loggerFactory.CreateLogger<TopDeskClient>()),
                "azure_devops" => new AzureDevOpsClient(config, new HttpClient(), _loggerFactory.CreateLogger<AzureDevOpsClient>()),
                _ => new MockClient(config, _loggerFactory.CreateLogger<MockClient>()),// Fallback or throw
            };
        }

        // Overload to force mock if needed, or we can handle it inside.
        public ISystemClient CreateMockClient(SystemConfig config)
        {
             return new MockClient(config, _loggerFactory.CreateLogger<MockClient>());
        }
    }
}
