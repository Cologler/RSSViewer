using System;

using RSSViewer.Abstractions;
using RSSViewer.StringMatchers;

namespace RSSViewer.Filter
{
    internal class StringMatcherRssItemFilter : IRssItemFilter
    {
        private readonly IStringMatcher _stringMatcher;

        public StringMatcherRssItemFilter(IStringMatcher stringMatcher)
        {
            this._stringMatcher = stringMatcher ?? throw new ArgumentNullException(nameof(stringMatcher));
        }

        public bool IsMatch(IPartialRssItem rssItem) => this._stringMatcher.IsMatch(rssItem.Title);
    }
}
