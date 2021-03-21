using System;
using System.Text.RegularExpressions;

namespace RSSViewer.Utils
{
    public static class RegexUtils
    {
        public static bool IsValidPattern(string pattern)
        {
            try
            {
                new Regex(pattern);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
