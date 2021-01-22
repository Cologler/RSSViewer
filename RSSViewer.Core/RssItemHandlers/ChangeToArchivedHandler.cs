using RSSViewer.Abstractions;

using System.Collections.Generic;
using System.Linq;

namespace RSSViewer.RssItemHandlers
{
    class ChangeToArchivedHandler : IRssItemHandler
    {
        public string Id => "cad27543-8029-4efa-8bb7-abec9868064e";

        public string HandlerName => "Change To Archived";

        public bool CanbeRuleTarget => false;

        public IAsyncEnumerable<(IPartialRssItem, RssItemState)> HandleAsync(IReadOnlyCollection<(IPartialRssItem, RssItemState)> rssItems)
        {
            return rssItems.Select(z => (z.Item1, RssItemState.Archived)).ToAsyncEnumerable();
        }
    }
}
