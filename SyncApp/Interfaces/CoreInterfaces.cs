using System.Collections.Generic;
using System.Threading.Tasks;

namespace SyncApp.Interfaces
{
    public interface ISystemClient
    {
        string SystemName { get; }
        Task<bool> ValidateFieldExistsAsync(string objectType, string fieldName);
        Task<IEnumerable<SyncItem>> GetChangesAsync(string objectType);
        Task<SyncItem?> GetItemByExternalIdAsync(string objectType, string externalIdField, string externalIdValue);
        Task<string> CreateItemAsync(string objectType, SyncItem item, string externalIdField, string externalIdValue);
        Task UpdateItemAsync(string objectType, string id, SyncItem item);
    }

    public class SyncItem
    {
        public required string Id { get; set; }
        public Dictionary<string, object> Fields { get; set; } = [];
    }

    public interface ITransformer
    {
        string Transform(string? value, string transformType);
    }
}
