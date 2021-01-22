using RSSViewer.Abstractions;

using System.Collections.Generic;
using System.Linq;

namespace RSSViewer.RssItemHandlers
{
    class ChangeUndecidedToArchivedHandler : IRssItemHandler
    {
        public string Id => "b2830422-8138-4456-9bac-65872a3266e0";

        public string HandlerName => "Change Undecided To Archived";

        public bool CanbeRuleTarget => false;

        public IAsyncEnumerable<(IPartialRssItem, RssItemState)> HandleAsync(IReadOnlyCollection<(IPartialRssItem, RssItemState)> rssItems)
        {
            return rssItems
                .Where(z => z.Item2 == RssItemState.Undecided)
                .Select(z => (z.Item1, RssItemState.Archived))
                .ToAsyncEnumerable();
        }
    }
}
