using Microsoft.AspNetCore.Components;
using System.Text.RegularExpressions;

namespace IgnisEducationSuite.Client.Services
{
    public static class LessonFormatter
    {
        public static MarkupString FormatLessonContent(string content)
        {
            // Step 1: Trim extra whitespace and normalize line breaks to '\n' (if content has mixed line endings)
            content = content.Trim();

            // Step 2: Replace double newlines with paragraph breaks for distinct sections
            content = Regex.Replace(content, @"\n\s*\n", "</p><p>");

            // Step 3: Replace remaining single newlines with line breaks for intra-paragraph line breaks
            content = content.Replace("\n", "<br>");

            // Step 4: Format section headers (e.g., "I. Introduction (10 minutes):")
            content = Regex.Replace(content, @"(I+|V+)\.\s(.*?)\((.*?)\):", "<h4>$1. $2 ($3)</h4>");

            // Step 5: Bold key terms (words ending with colon), more specific to avoid unintended matches
            content = Regex.Replace(content, @"\b([A-Z][a-zA-Z]+):", "<strong>$1:</strong>");

            // Wrap the formatted content in a <p> tag and return it
            return new MarkupString("<p>" + content + "</p>");
        }
    }
}
