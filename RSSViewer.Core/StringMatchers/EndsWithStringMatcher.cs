using System;

namespace RSSViewer.StringMatchers
{
    class EndsWithStringMatcher : IStringMatcher
    {
        private readonly string _suffix;
        private readonly StringComparison _comparison;

        public EndsWithStringMatcher(string suffix, StringComparison comparison)
        {
            if (String.IsNullOrEmpty(suffix))
                throw new ArgumentException("message", nameof(suffix));
            this._suffix = suffix;
            this._comparison = comparison;
        }

        public bool IsMatch(string value) => value.EndsWith(this._suffix, this._comparison);
    }
}
