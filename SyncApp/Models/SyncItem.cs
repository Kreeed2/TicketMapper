namespace SyncApp.Models;

public class SyncItem
{
    public required string Id { get; set; }
    public Dictionary<string, object> Fields { get; set; } = [];
    public Dictionary<string, object> ForeignFields { get; set; } = [];
}
