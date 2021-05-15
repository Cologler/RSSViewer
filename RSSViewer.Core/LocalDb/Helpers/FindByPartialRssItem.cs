
using RSSViewer.Abstractions;

namespace RSSViewer.LocalDb.Helpers
{
    class FindByPartialRssItem : IRssItemFinder<IPartialRssItem>
    {
        public RssItem FindRssItem(LocalDbContext context, IPartialRssItem fromItem) => 
            context.RssItems.Find(fromItem.FeedId, fromItem.RssId);
    }
}
