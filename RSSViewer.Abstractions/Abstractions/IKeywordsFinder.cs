using System.Collections.Generic;

namespace RSSViewer.Abstractions
{
    public interface IKeywordsFinder
    {
        IEnumerable<string> GetKeywords(IRssItem item);
    }
}
