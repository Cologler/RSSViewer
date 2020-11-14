using RSSViewer.Abstractions;

using System.Collections.Generic;
using System.Linq;

namespace RSSViewer.RssItemHandlers
{
    class ChangeUndecidedToAcceptedHandler : IRssItemHandler
    {
        public string HandlerName => "Change Undecided To Accepted";

        public IAsyncEnumerable<(IRssItem, RssItemState)> Accept(IReadOnlyCollection<(IRssItem, RssItemState)> rssItems)
        {
            return rssItems
                .Where(z => z.Item2 == RssItemState.Undecided)
                .Select(z => (z.Item1, RssItemState.Accepted))
                .ToAsyncEnumerable();
        }
    }
}
