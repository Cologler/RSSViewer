namespace RSSViewer.Abstractions
{
    public interface IPartialRssItem
    {
        string FeedId { get; }

        string RssId { get; }

        string Title { get; }

        bool TryGetProperty(RssItemProperties property, out string value);
    }
}
