using RSSViewer.Abstractions;

using System.Collections.Generic;
using System.Linq;

namespace RSSViewer.RssItemHandlers
{
    class ChangeUndecidedToAcceptedHandler : IRssItemHandler
    {
        public string Id => "2e09db60-8ac2-4d29-879a-670a279a9c80";

        public string HandlerName => "Change Undecided To Accepted";

        public bool CanbeRuleTarget => false;

        public IAsyncEnumerable<(IRssItem, RssItemState)> Accept(IReadOnlyCollection<(IRssItem, RssItemState)> rssItems)
        {
            return rssItems
                .Where(z => z.Item2 == RssItemState.Undecided)
                .Select(z => (z.Item1, RssItemState.Accepted))
                .ToAsyncEnumerable();
        }
    }
}
