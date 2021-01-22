namespace RSSViewer.Abstractions
{
    public interface IPartialRssItem
    {
        string FeedId { get; }

        string RssId { get; }

        string Title { get; }

        public RssItemState State { get; set; }

        bool TryGetProperty(RssItemProperties property, out string value);
    }
}
