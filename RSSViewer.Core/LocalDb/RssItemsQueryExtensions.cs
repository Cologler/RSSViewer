using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSSViewer.LocalDb
{
    public static class RssItemsQueryExtensions
    {
        public static IQueryable<RssItem> WithFeedId(this IQueryable<RssItem> queryable, string feedId)
        {
            if (queryable is null)
                throw new ArgumentNullException(nameof(queryable));

            return feedId is null ? queryable : queryable.Where(z => z.FeedId == feedId);
        }

        public static IQueryable<RssItem> WithStates(this IQueryable<RssItem> queryable, RssItemState[] states)
        {
            if (queryable is null)
                throw new ArgumentNullException(nameof(queryable));
            if (states is null)
                throw new ArgumentNullException(nameof(states));

            Debug.Assert(states.Distinct().Count() == states.Length);

            switch (states.Length)
            {
                case 0:
                    return queryable;

                case 1:
                    return queryable.Where(z => z.State == states[0]);

                case 2:
                    return queryable.Where(z => z.State == states[0] || z.State == states[1]);

                case 3:
                    return queryable.Where(z => z.State == states[0] || z.State == states[1] || z.State == states[2]);

                default:
                    throw new NotImplementedException();
            }
        }

        internal static IQueryable<PartialRssItem> ToPartialRssItem(this IQueryable<RssItem> queryable)
        {
            if (queryable is null)
                throw new ArgumentNullException(nameof(queryable));

            return queryable
                   .Select(z => new PartialRssItem
                   {
                       FeedId = z.FeedId,
                       RssId = z.RssId,
                       State = z.State,
                       Title = z.Title,
                       MagnetLink = z.MagnetLink
                   });
        }
    }
}
