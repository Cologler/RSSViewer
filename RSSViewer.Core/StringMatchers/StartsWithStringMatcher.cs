using System;

namespace RSSViewer.StringMatchers
{
    class StartsWithStringMatcher : IStringMatcher
    {
        private readonly string _prefix;
        private readonly StringComparison _comparison;

        public StartsWithStringMatcher(string prefix, StringComparison comparison)
        {
            if (String.IsNullOrEmpty(prefix))
                throw new ArgumentException("message", nameof(prefix));
            this._prefix = prefix;
            this._comparison = comparison;
        }

        public bool IsMatch(string value) => value.StartsWith(this._prefix, this._comparison);
    }
}
