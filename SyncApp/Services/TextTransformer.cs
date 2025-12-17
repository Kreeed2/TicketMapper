using SyncApp.Interfaces;
using SyncApp.Models;
using System.Text.RegularExpressions;

namespace SyncApp.Services
{
    public class TextTransformer() : ITransformer
    {
        public string? Transform(string? value, FieldMappingTransform transformType)
        {
            if (string.IsNullOrEmpty(value)) return null;

            return (transformType) switch
            {
                FieldMappingTransform.HtmlToMarkdown => ConvertHtmlToMarkdown(value),
                _ => value,
            };
        }

        private static string ConvertHtmlToMarkdown(string html)
        {
            // Very basic implementation for demonstration
            var markdown = html;
            markdown = Regex.Replace(markdown, "<b>(.*?)</b>", "**$1**");
            markdown = Regex.Replace(markdown, "<i>(.*?)</i>", "*$1*");
            markdown = Regex.Replace(markdown, "<br\\s*/?>", "\n");
            // ... add more rules or use a library like ReverseMarkdown
            return markdown;
        }
    }
}
