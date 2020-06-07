using RSSViewer.LocalDb;
using System;
using System.Collections.Generic;
using System.Text;

namespace RSSViewer.Abstractions
{
    public interface IRssItemsStateChangedInfo
    {
        IEnumerable<RssItem> GetItems(RssItemState newState);
    }
}
