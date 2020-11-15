using System;
using System.Collections.Generic;
using System.Text;

using RSSViewer.Abstractions;
using RSSViewer.Utils;

namespace RSSViewer
{
    public static class EventNames
    {
        public static readonly TypedEventName<IReadOnlyCollection<(IRssItem, RssItemState)>> RssItemsStateChanged =
            new(nameof(RssItemsStateChanged));

        public static readonly TypedEventName<IReadOnlyCollection<IRssItem>> AddedRssItems =
            new(nameof(AddedRssItems));
    }
}
