using System;
using System.Text;

namespace RSSViewer.Abstractions
{
    public interface IRssItem
    {
        string FeedId { get; }

        string RssId { get; }

        string Title { get; }

        string Description { get; }

        string GetProperty(RssItemProperties property);
    }
}
