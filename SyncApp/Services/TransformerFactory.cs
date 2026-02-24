using SyncApp.Interfaces;
using SyncApp.Models;

namespace SyncApp.Services;

public class TransformerFactory(IServiceProvider pServiceProvider)
{
    SystemMappingType mSourceSystem;
    SystemMappingType mTargetSystem;

    public void Configure(SystemMappingType pSourceSystem, SystemMappingType pTargetSystem)
    {
        mSourceSystem = pSourceSystem;
        mTargetSystem = pTargetSystem;
    }

    public ITransformer CreateTransformer(FieldMapping pFieldMapping)
    {
        switch (pFieldMapping.Transform)
        {
            case FieldMappingTransform.None:
            case FieldMappingTransform.Static:
            case FieldMappingTransform.HtmlToMarkdown:
            case FieldMappingTransform.ValueMap:
                var textService = pServiceProvider.GetRequiredService<TextTransformer>();
                return textService.Configure(mSourceSystem, mTargetSystem, pFieldMapping);

            case FieldMappingTransform.Lookup:
            case FieldMappingTransform.Pattern:
                var service = pServiceProvider.GetRequiredService<UserTransformer>();
                return service.Configure(mSourceSystem, mTargetSystem, pFieldMapping);
            default:
                throw new NotImplementedException();
        }
    }
}
