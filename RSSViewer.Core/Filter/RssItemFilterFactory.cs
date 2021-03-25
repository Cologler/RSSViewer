using RSSViewer.Configuration;
using RSSViewer.Filter;
using RSSViewer.RulesDb;
using RSSViewer.StringMatchers;
using RSSViewer.Utils;

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace RSSViewer.Filter
{
    public class RssItemFilterFactory
    {
        private readonly RegexCache _regexCache;
        private readonly AllRssItemFilter _allRssItemFilter = new();

        public RssItemFilterFactory(RegexCache regexCache)
        {
            this._regexCache = regexCache;
        }

        public IRssItemFilter Create(MatchRule rule)
        {
            if (rule is null)
                throw new ArgumentNullException(nameof(rule));

            if (rule.Mode.IsStringMode())
            {
                return new StringMatcherRssItemFilter(this.CreateStringMatcher(rule.CreateStringMatchArguments()));
            }

            return rule.Mode switch
            {
                MatchMode.None => throw new NotImplementedException(),
                MatchMode.All => this._allRssItemFilter,
                _ => throw new InvalidOperationException()
            };
        }

        IStringMatcher CreateStringMatcher(StringMatchArguments rule)
        {
            switch (rule.Mode)
            {
                case StringMatchMode.Contains:
                    return new ContainsStringMatcher(rule.Value, rule.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
                case StringMatchMode.StartsWith:
                    return new StartsWithStringMatcher(rule.Value, rule.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
                case StringMatchMode.EndsWith:
                    return new EndsWithStringMatcher(rule.Value, rule.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
                case StringMatchMode.Wildcard:
                    return new RegexStringMatcher(
                        this._regexCache.TryGet(
                            WildcardUtils.ToRegexPattern(rule.Value),
                            rule.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None));
                case StringMatchMode.Regex:
                    return new RegexStringMatcher(
                        this._regexCache.TryGet(
                            rule.Value,
                            rule.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None));
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
