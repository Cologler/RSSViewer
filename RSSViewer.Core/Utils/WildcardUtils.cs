using System;
using System.Text;
using System.Text.RegularExpressions;

namespace RSSViewer.Utils
{
    public static class WildcardUtils
    {
        public static readonly RegexOptions WildcardRegexOptions = RegexOptions.IgnoreCase;

        public static Regex WildcardToRegex(string wildcardText) => new Regex(WildcardEscape(wildcardText), WildcardRegexOptions);

        public static string WildcardEscape(string wildcardText)
        {
            if (wildcardText is null)
                throw new ArgumentNullException(nameof(wildcardText));

            var any = ".";
            return Regex.Escape(wildcardText)
                .Replace("\\*", any + "*")
                .Replace("\\?", any);
        }
    }
}
