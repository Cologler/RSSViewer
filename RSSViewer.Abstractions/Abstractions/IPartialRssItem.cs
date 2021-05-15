namespace RSSViewer.Abstractions
{
    public interface IPartialRssItem : IRssItemKey
    {
        string Title { get; }

        public RssItemState State { get; set; }

        bool TryGetProperty(RssItemProperties property, out string value);
    }
}
