using System.Linq;

using RSSViewer.LocalDb;

namespace RSSViewer.Search
{
    internal interface IDbSearchPart : ISearchPart
    {
        IQueryable<RssItem> Where(IQueryable<RssItem> queryable);
    }
}
