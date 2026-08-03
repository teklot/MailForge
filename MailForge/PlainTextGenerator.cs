using System.Net;
using System.Text.RegularExpressions;

namespace MailForge
{
    /// <summary>
    /// Converts an HTML body into a readable plain-text version by inserting line breaks at block
    /// boundaries, stripping tags, and decoding HTML entities.
    /// </summary>
    public static class PlainTextGenerator
    {
        private static readonly Regex BlockBoundary = new Regex(
            @"(?i)<\s*(br|hr|/p|/div|/tr|/li|/h[1-6]|p|div|tr|li|h[1-6]|table|ul|ol|section|header|footer)\b[^>]*>",
            RegexOptions.Compiled);

        private static readonly Regex RemainingTags = new Regex(@"<[^>]+>", RegexOptions.Compiled);

        private static readonly Regex CollapseSpaces = new Regex(@"[ \t]+", RegexOptions.Compiled);

        private static readonly Regex CollapseBlankLines = new Regex(@"\n{3,}", RegexOptions.Compiled);

        /// <summary>Generates a plain-text body from an HTML body.</summary>
        public static string Generate(string html)
        {
            if (string.IsNullOrEmpty(html))
                return html;

            var text = BlockBoundary.Replace(html, "\n");
            text = RemainingTags.Replace(text, string.Empty);
            text = WebUtility.HtmlDecode(text);
            text = CollapseSpaces.Replace(text, " ");
            text = CollapseBlankLines.Replace(text, "\n\n");
            return text.Trim();
        }
    }
}
