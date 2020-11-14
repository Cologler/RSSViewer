using System;
using System.Collections.Generic;
using System.Text;

using RSSViewer.Abstractions;
using RSSViewer.Utils;

namespace RSSViewer
{
    public static class EventNames
    {
        public static readonly TypedEventName<IEnumerable<(IRssItem, RssItemState)>> RssItemsStateChanged =
            new TypedEventName<IEnumerable<(IRssItem, RssItemState)>>(nameof(RssItemsStateChanged));
    }
}
