using SyncApp.Models;

namespace SyncApp.Interfaces;

public interface ITransformer
{
    ITransformer Configure(SystemMappingType pSourceSystem, SystemMappingType pTargetSystem, FieldMapping pMapping);

    Task<string?> Transform(string? pValue, FieldMappingTransform pTransformType);
}
