using RSSViewer.Abstractions;
using RSSViewer.LocalDb;

using System;
using System.Collections.Generic;
using System.Text;

namespace RSSViewer.RssItemHelper
{
    public static class RssItemsStateChangedInfo
    {
        public static IRssItemsStateChangedInfo CreateAccepted(IEnumerable<RssItem> rssItems)
        {
            if (rssItems is null)
                throw new ArgumentNullException(nameof(rssItems));

            var ss = new SingleStateRssItemsStateChangedInfo(RssItemState.Accepted);
            ss.Items.AddRange(rssItems);
            return ss;
        }

        public static IRssItemsStateChangedInfo CreateRejected(IEnumerable<RssItem> rssItems)
        {
            if (rssItems is null)
                throw new ArgumentNullException(nameof(rssItems));

            var ss = new SingleStateRssItemsStateChangedInfo(RssItemState.Rejected);
            ss.Items.AddRange(rssItems);
            return ss;
        }

        class SingleStateRssItemsStateChangedInfo : IRssItemsStateChangedInfo
        {
            private readonly RssItemState _state;

            public SingleStateRssItemsStateChangedInfo(RssItemState state)
            {
                this._state = state;
            }

            public List<RssItem> Items { get; } = new List<RssItem>();

            public IEnumerable<RssItem> GetItems(RssItemState newState)
            {
                if (newState == this._state)
                {
                    return this.Items;
                }

                return Array.Empty<RssItem>();
            }
        }
    }
}
