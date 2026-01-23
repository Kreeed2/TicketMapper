using SyncApp.Models;

namespace SyncApp.Interfaces;

public interface ISystemClient
{
    Task<bool> ValidateFieldExistsAsync(string objectType, string fieldName);
    Task<IEnumerable<SyncItem>> GetChangesAsync(string objectType);
    Task<SyncItem?> GetItemByExternalIdAsync(string objectType, string externalIdField, string externalIdValue);
    Task<string> CreateItemAsync(string objectType, SyncItem item, string externalIdField, string externalIdValue);
    Task UpdateItemAsync(string objectType, string id, SyncItem item);
}
