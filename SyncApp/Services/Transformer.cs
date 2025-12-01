using System.Text.RegularExpressions;
using SyncApp.Interfaces;

namespace SyncApp.Services
{
    public class Transformer : ITransformer
    {
        public string Transform(string? value, string transformType)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            switch (transformType?.ToLower())
            {
                case "html_to_markdown":
                    return ConvertHtmlToMarkdown(value);
                case "none":
                default:
                    return value;
            }
        }

        private string ConvertHtmlToMarkdown(string html)
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
