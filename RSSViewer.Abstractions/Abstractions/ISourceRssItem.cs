using System.Collections.Generic;

namespace RSSViewer.Abstractions
{
    public interface ISourceRssItem
    {
        string FeedId { get; }

        string RssId { get; }

        string RawText { get; }

        string GetProperty(RssItemProperties property);
    }
}
