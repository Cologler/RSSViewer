using RSSViewer.Abstractions;

using System.Collections.Generic;
using System.Linq;

namespace RSSViewer.RssItemHandlers
{
    class ChangeToRejectedHandler : IRssItemHandler
    {
        public string Id => "477406ca-9839-4673-84d1-17987b0198e7";

        public string HandlerName => "Change To Rejected";

        public bool CanbeRuleTarget => true;

        public IAsyncEnumerable<(IPartialRssItem, RssItemState)> HandleAsync(IReadOnlyCollection<(IPartialRssItem, RssItemState)> rssItems)
        {
            return rssItems.Select(z => (z.Item1, RssItemState.Rejected)).ToAsyncEnumerable();
        }
    }
}
