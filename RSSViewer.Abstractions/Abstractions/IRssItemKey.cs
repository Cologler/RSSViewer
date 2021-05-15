namespace RSSViewer.Abstractions
{
    public interface IRssItemKey
    {
        string FeedId { get; }

        string RssId { get; }

        public (string, string) ToTuple() => (this.FeedId, this.RssId);
    }
}
