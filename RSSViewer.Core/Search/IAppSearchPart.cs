using System.Collections.Generic;

namespace RSSViewer.Search
{
    internal interface IAppSearchPart : ISearchPart
    {
        IEnumerable<PartialRssItem> Where(IEnumerable<PartialRssItem> enumerable);
    }
}
