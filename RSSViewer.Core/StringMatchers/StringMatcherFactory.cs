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

            return this.Create(rule.CreateStringMatchArguments());
        }

        public IStringMatcher Create(StringMatchArguments rule)
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
