using RSSViewer.Abstractions;

namespace RSSViewer.LocalDb.Helpers
{
    class RssItemOldStateSnapshot : RssItemStateSnapshot, IRssItemKey
    {
        public string FeedId { get; set; }

        public string RssId { get; set; }

        public override void UpdateFrom(RssItem rssItem)
        {
            this.FeedId = rssItem.FeedId;
            this.RssId = rssItem.RssId;
            base.UpdateFrom(rssItem);
        }
    }
}
