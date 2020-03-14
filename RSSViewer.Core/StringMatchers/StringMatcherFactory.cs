using RSSViewer.Configuration;
using RSSViewer.Utils;
using System;
using System.Text.RegularExpressions;

namespace RSSViewer.StringMatchers
{
    class StringMatcherFactory
    {
        public IStringMatcher Create(MatchStringConf conf)
        {
            if (conf is null)
                throw new ArgumentNullException(nameof(conf));

            switch (conf.MatchMode)
            {
                case MatchStringMode.Contains:
                    return new ContainsStringMatcher(conf.MatchValue, (StringComparison)conf.MatchOptions);
                case MatchStringMode.StartsWith:
                    return new StartsWithStringMatcher(conf.MatchValue, (StringComparison)conf.MatchOptions);
                case MatchStringMode.EndsWith:
                    return new EndsWithStringMatcher(conf.MatchValue, (StringComparison)conf.MatchOptions);
                case MatchStringMode.Wildcard:
                    return new RegexStringMatcher(RegexUtils.WildcardToRegex(conf.MatchValue));
                case MatchStringMode.Regex:
                    return new RegexStringMatcher(new Regex(conf.MatchValue, (RegexOptions)conf.MatchOptions));
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
