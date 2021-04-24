using System;

using RSSViewer.Abstractions;
using RSSViewer.Models;
using RSSViewer.StringMatchers;

namespace RSSViewer.Filter
{
    class StringMatcherRssItemFilter : IRssItemFilter
    {
        private readonly IStringMatcher _stringMatcher;

        public StringMatcherRssItemFilter(IStringMatcher stringMatcher)
        {
            this._stringMatcher = stringMatcher ?? throw new ArgumentNullException(nameof(stringMatcher));
        }

        public bool IsMatch(ClassifyContext<IPartialRssItem> context) => this._stringMatcher.IsMatch(context.Item.Title);
    }
}
