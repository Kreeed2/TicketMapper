using System.Text.RegularExpressions;
using SyncApp.Interfaces;

namespace SyncApp.Services
{
    public class Transformer(IUserMappingService userMappingService) : ITransformer
    {
        public string? Transform(string? value, string transformType)
        {
            if (string.IsNullOrEmpty(value)) return null;

            return (transformType?.ToLower()) switch
            {
                "html_to_markdown" => ConvertHtmlToMarkdown(value),
                "user_map" => userMappingService.MapUser(value),
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
