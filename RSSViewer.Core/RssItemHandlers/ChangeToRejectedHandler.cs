using RSSViewer.Abstractions;

using System.Collections.Generic;
using System.Linq;

namespace RSSViewer.RssItemHandlers
{
    class ChangeToRejectedHandler : IRssItemHandler
    {
        public string HandlerName => "Change To Rejected";

        public IAsyncEnumerable<(IRssItem, RssItemState)> Accept(IReadOnlyCollection<(IRssItem, RssItemState)> rssItems)
        {
            return rssItems.Select(z => (z.Item1, RssItemState.Rejected)).ToAsyncEnumerable();
        }
    }
}
