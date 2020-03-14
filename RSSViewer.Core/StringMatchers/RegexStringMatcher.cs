using System;
using System.Text.RegularExpressions;

namespace RSSViewer.StringMatchers
{
    class RegexStringMatcher : IStringMatcher
    {
        private readonly Regex _regex;

        public RegexStringMatcher(Regex regex)
        {
            this._regex = regex ?? throw new ArgumentNullException(nameof(regex));
        }

        public bool IsMatch(string value) => this._regex.IsMatch(value);
    }
}
