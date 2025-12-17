using SyncApp.Models;

namespace SyncApp.Interfaces;

public interface IUserService
{
    Task<SyncItem?> LookupUserByEmailAsync(string email);
    Task<SyncItem?> LookupUserByDisplayNameAsync(string displayName);
    Task<SyncItem?> LookupUserBySystemIdAsync(string systemId);
}
