using RSSViewer.Abstractions;

using System.Collections.Generic;
using System.Linq;

namespace RSSViewer.RssItemHandlers
{
    class ChangeUndecidedToRejectedHandler : IRssItemHandler
    {
        public string Id => "46d73fe1-77c4-446b-82f8-eb1fbae7a3ff";

        public string HandlerName => "Change Undecided To Rejected";

        public IAsyncEnumerable<(IRssItem, RssItemState)> Accept(IReadOnlyCollection<(IRssItem, RssItemState)> rssItems)
        {
            return rssItems
                .Where(z => z.Item2 == RssItemState.Undecided)
                .Select(z => (z.Item1, RssItemState.Rejected))
                .ToAsyncEnumerable();
        }
    }
}
