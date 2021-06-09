using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using RSSViewer.LocalDb;

namespace RSSViewer.Search
{
    internal static class RssItemsQuerySearchExtensions
    {
        public static IQueryable<RssItem> WithDbSideFilter(this IQueryable<RssItem> queryable, SearchExpression expr)
        {
            if (queryable is null)
                throw new ArgumentNullException(nameof(queryable));
            if (expr is null)
                throw new ArgumentNullException(nameof(expr));

            foreach (var part in expr.Parts.OfType<IDbSearchPart>())
            {
                queryable = part.Where(queryable);
            }

            return queryable;
        }

        public static IEnumerable<PartialRssItem> WithClientSideFilter(this IEnumerable<PartialRssItem> queryable, SearchExpression expr)
        {
            if (queryable is null)
                throw new ArgumentNullException(nameof(queryable));
            if (expr is null)
                throw new ArgumentNullException(nameof(expr));

            foreach (var part in expr.Parts.OfType<IAppSearchPart>())
            {
                queryable = part.Where(queryable);
            }

            return queryable;
        }
    }
}
