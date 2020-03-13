using RSSViewer.Abstractions;

namespace RSSViewer.LocalDb
{
    public class RssItem
    {
        public string FeedId { get; set; }

        public string RssId { get; set; }

        public RssStates State { get; set; }

        public string RawText { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Link { get; set; }

        public string MagnetLink { get; set; }

        public void UpdateFrom(ISourceRssItem sourceRssItem)
        {
            this.FeedId = sourceRssItem.FeedId;
            this.RssId = sourceRssItem.RssId;
            this.RawText = sourceRssItem.RawText;

            this.Title = sourceRssItem.GetProperty(RssItemProperties.Title);
            this.Description = sourceRssItem.GetProperty(RssItemProperties.Description);
            this.Link = sourceRssItem.GetProperty(RssItemProperties.Link);
            this.MagnetLink = sourceRssItem.GetProperty(RssItemProperties.MagnetLink);
        }
    }
}
