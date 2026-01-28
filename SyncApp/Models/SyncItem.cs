namespace SyncApp.Models;

public class SyncItem
{
    public required string Id { get; set; }
    public Dictionary<string, object> Fields { get; set; } = [];
    public Dictionary<string, IEnumerable<SyncItem>> ForeignFields { get; set; } = [];
}
