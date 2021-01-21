using RSSViewer.Configuration;
using RSSViewer.RulesDb;
using RSSViewer.Utils;
using System;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace RSSViewer.StringMatchers
{
    public class StringMatcherFactory
    {
        private ImmutableDictionary<(string, RegexOptions), Regex> _regexesCache = ImmutableDictionary<(string, RegexOptions), Regex>.Empty;

        private Regex GetOrCreateRegex(string pattern, RegexOptions options)
        {
            var key = (pattern, options);
            if (!this._regexesCache.TryGetValue(key, out var r))
            {
                r = new Regex(pattern, options);
                this._regexesCache = this._regexesCache.SetItem(key, r);
            }
            return r;
        }

        private Regex GetOrCreateWildcardRegex(string value)
        {
            var key = (value, RegexUtils.WildcardRegexOptions);
            if (!this._regexesCache.TryGetValue(key, out var r))
            {
                r = RegexUtils.WildcardToRegex(value);
                this._regexesCache = this._regexesCache.SetItem(key, r);
            }
            return r;
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
                    return new RegexStringMatcher(this.GetOrCreateWildcardRegex(rule.Argument));
                case MatchMode.Regex:
                    return new RegexStringMatcher(this.GetOrCreateRegex(rule.Argument, rule.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None));
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
