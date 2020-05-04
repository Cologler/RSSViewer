using System;
using System.Text;
using System.Text.RegularExpressions;

namespace RSSViewer.Utils
{
    public static class RegexUtils
    {
        public static Regex WildcardToRegex(string wildcardText)
        {
            var any = ".";
            var regex = new Regex(
                Regex.Escape(wildcardText)
                    .Replace("\\*", any + "*")
                    .Replace("\\?", any),
                RegexOptions.IgnoreCase
            );
            return regex;
        }
    }
}
