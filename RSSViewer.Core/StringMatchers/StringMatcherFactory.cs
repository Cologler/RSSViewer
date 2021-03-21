using RSSViewer.Configuration;
using RSSViewer.RulesDb;
using RSSViewer.Utils;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace RSSViewer.StringMatchers
{
    public class StringMatcherFactory
    {
        private readonly RegexCache _regexCache;

        public StringMatcherFactory(RegexCache regexCache)
        {
            this._regexCache = regexCache;
        }

        public IStringMatcher Create(MatchRule rule)
        {
            if (rule is null)
                throw new ArgumentNullException(nameof(rule));

            switch (rule.Mode)
            {
                case MatchMode.Contains:
                    return new ContainsStringMatcher(rule.Argument, rule.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
                case MatchMode.StartsWith:
                    return new StartsWithStringMatcher(rule.Argument, rule.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
                case MatchMode.EndsWith:
                    return new EndsWithStringMatcher(rule.Argument, rule.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
                case MatchMode.Wildcard:
                    return new RegexStringMatcher(
                        this._regexCache.TryGet(
                            WildcardUtils.ToRegexPattern(rule.Argument), 
                            rule.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None));
                case MatchMode.Regex:
                    return new RegexStringMatcher(
                        this._regexCache.TryGet(
                            rule.Argument, 
                            rule.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None));
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
