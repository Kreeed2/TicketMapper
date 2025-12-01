using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SyncApp.Interfaces;
using SyncApp.Models;

namespace SyncApp.Services
{
    public class MockClient : ISystemClient
    {
        private readonly SystemConfig _config;
        private readonly ILogger _logger;
        public string SystemName => "mock";

        // Simple in-memory store for dry-run verification
        public static List<SyncItem> InMemoryStore = new List<SyncItem>();

        public MockClient(SystemConfig config, ILogger logger)
        {
            _config = config;
            _logger = logger;
        }

        public Task<bool> ValidateFieldExistsAsync(string objectType, string fieldName)
        {
            _logger.LogInformation($"[Mock] Validating field {fieldName} exists on {objectType}");
            return Task.FromResult(true);
        }

        public Task<IEnumerable<SyncItem>> GetChangesAsync(string objectType)
        {
            _logger.LogInformation($"[Mock] Fetching changes for {objectType} from {_config.Url}");

            // Return some dummy data if InMemoryStore is empty, otherwise return store
            if (!InMemoryStore.Any())
            {
                var dummy = new SyncItem { Id = "1001" };
                dummy.Fields["briefDescription"] = "Mock Incident 1001";
                dummy.Fields["request"] = "<b>Help me</b>";
                dummy.Fields["System.Title"] = "Mock Bug 1001";
                dummy.Fields["System.Description"] = "Fix it";

                return Task.FromResult<IEnumerable<SyncItem>>(new List<SyncItem> { dummy });
            }

            return Task.FromResult<IEnumerable<SyncItem>>(InMemoryStore);
        }

        public Task<SyncItem?> GetItemByExternalIdAsync(string objectType, string externalIdField, string externalIdValue)
        {
            _logger.LogInformation($"[Mock] Searching {objectType} where {externalIdField} == {externalIdValue}");
            var item = InMemoryStore.FirstOrDefault(x => x.Fields.ContainsKey(externalIdField) && x.Fields[externalIdField].ToString() == externalIdValue);
            return Task.FromResult<SyncItem?>(item);
        }

        public Task<string> CreateItemAsync(string objectType, SyncItem item, string externalIdField, string externalIdValue)
        {
            string newId = (InMemoryStore.Count + 2000).ToString();
            _logger.LogInformation($"[Mock] Creating {objectType} with ID {newId}. ExternalID: {externalIdValue}");

            item.Id = newId;
            item.Fields[externalIdField] = externalIdValue;
            InMemoryStore.Add(item);

            return Task.FromResult(newId);
        }

        public Task UpdateItemAsync(string objectType, string id, SyncItem item)
        {
            _logger.LogInformation($"[Mock] Updating {objectType} ID {id}");
            var existing = InMemoryStore.FirstOrDefault(x => x.Id == id);
            if (existing != null)
            {
                foreach(var kvp in item.Fields)
                {
                    existing.Fields[kvp.Key] = kvp.Value;
                }
            }
            return Task.CompletedTask;
        }
    }
}
