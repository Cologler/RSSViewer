using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using RSSViewer.RulesDb;
using RSSViewer.Utils;

namespace RSSViewer.Helpers
{
    public static class RegexHelper
    {
        public static string ConvertToRegexPattern(MatchMode matchMode, string value)
        {
            switch (matchMode)
            {
                case MatchMode.None:
                    throw new InvalidOperationException();

                case MatchMode.Contains:
                    return Regex.Escape(value);

                case MatchMode.StartsWith:
                    return "^" + Regex.Escape(value);

                case MatchMode.EndsWith:
                    return Regex.Escape(value) + "$";

                case MatchMode.Wildcard:
                    return WildcardUtils.ToRegexPattern(value);

                case MatchMode.Regex:
                    return value;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
