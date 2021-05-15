using RSSViewer.Abstractions;

namespace RSSViewer.LocalDb.Helpers
{
    class RssItemOldStateSnapshot : RssItemStateSnapshot
    {
        public string FeedId { get; set; }

        public string RssId { get; set; }

        public override void UpdateFrom(RssItem rssItem)
        {
            this.FeedId = rssItem.FeedId;
            this.RssId = rssItem.RssId;
            base.UpdateFrom(rssItem);
        }

        public class Finder : IRssItemFinder<RssItemOldStateSnapshot>
        {
            public RssItem FindRssItem(LocalDbContext context, RssItemOldStateSnapshot fromItem) =>
                context.RssItems.Find(fromItem.FeedId, fromItem.RssId);
        }
    }
}
