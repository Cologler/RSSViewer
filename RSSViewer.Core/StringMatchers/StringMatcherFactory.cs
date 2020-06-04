using RSSViewer.Configuration;
using RSSViewer.RulesDb;
using RSSViewer.Utils;
using System;
using System.Text.RegularExpressions;

namespace RSSViewer.StringMatchers
{
    public class StringMatcherFactory
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

        public IStringMatcher Create(MatchRule rule)
        {
            if (rule is null)
                throw new ArgumentNullException(nameof(rule));

            switch (rule.Mode)
            {
                case MatchMode.Contains:
                    return new ContainsStringMatcher(rule.Argument, (StringComparison)rule.ExtraOptions);
                case MatchMode.StartsWith:
                    return new StartsWithStringMatcher(rule.Argument, (StringComparison)rule.ExtraOptions);
                case MatchMode.EndsWith:
                    return new EndsWithStringMatcher(rule.Argument, (StringComparison)rule.ExtraOptions);
                case MatchMode.Wildcard:
                    return new RegexStringMatcher(RegexUtils.WildcardToRegex(rule.Argument));
                case MatchMode.Regex:
                    return new RegexStringMatcher(new Regex(rule.Argument, (RegexOptions)rule.ExtraOptions));
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
