using RSSViewer.Abstractions;

using System;
using System.Collections.Generic;
using System.Linq;

namespace RSSViewer.RssItemHandlers
{
    /// <summary>
    /// Do nothing. this is use to group rules.
    /// </summary>
    class EmptyHandler : IRssItemHandler
    {
        public string Id => KnownHandlerIds.EmptyHandlerId;

        public string HandlerName => "Do Nothing";

        public bool CanbeRuleTarget => true;

        public IAsyncEnumerable<(IPartialRssItem, RssItemState)> HandleAsync(IReadOnlyCollection<(IPartialRssItem, RssItemState)> rssItems)
        {
            return AsyncEnumerable.Empty<(IPartialRssItem, RssItemState)>();
        }
    }
}
