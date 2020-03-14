using System;

namespace RSSViewer.StringMatchers
{
    class EndsWithStringMatcher : IStringMatcher
    {
        private readonly string _suffix;
        private readonly StringComparison _comparison;

        public EndsWithStringMatcher(string suffix, StringComparison comparison)
        {
            this._suffix = suffix ?? throw new ArgumentNullException(nameof(suffix));
            this._comparison = comparison;
        }

        public bool IsMatch(string value) => value.EndsWith(this._suffix, this._comparison);
    }
}
