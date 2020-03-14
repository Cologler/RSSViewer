using System;

namespace RSSViewer.StringMatchers
{
    class ContainsStringMatcher : IStringMatcher
    {
        private readonly string _subString;
        private readonly StringComparison _comparison;

        public ContainsStringMatcher(string subString, StringComparison comparison)
        {
            if (String.IsNullOrEmpty(subString))
                throw new ArgumentException("message", nameof(subString));
            this._subString = subString;
            this._comparison = comparison;
        }

        public bool IsMatch(string value) => value.Contains(this._subString, this._comparison);
    }
}
