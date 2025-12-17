using SyncApp.Interfaces;
using SyncApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncApp.Services
{
    public class TransformerFactory(IServiceProvider pServiceProvider, ILoggerFactory pLoggerFactory)
    {
        public ITransformer CreateTransformer(FieldMapping pFieldMapping)
        {
            return pFieldMapping.Transform switch
            {
                FieldMappingTransform.None
                or FieldMappingTransform.Static
                or FieldMappingTransform.HtmlToMarkdown => pServiceProvider.GetRequiredService<TextTransformer>(),
                FieldMappingTransform.Pattern
                or FieldMappingTransform.Lookup => pServiceProvider.GetRequiredService<UserTransformer>(),
                _ => throw new NotImplementedException()
            };
        }
    }
}
