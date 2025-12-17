using SyncApp.Models;

namespace SyncApp.Interfaces;

public interface ITransformer
{
    string? Transform(string? value, FieldMappingTransform transformType);
}
