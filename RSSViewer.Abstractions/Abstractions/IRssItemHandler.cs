using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RSSViewer.Abstractions
{
    public interface IRssItemHandler
    {
        string HandlerName { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rssItems"></param>
        /// <returns>the new state to change</returns>
        IAsyncEnumerable<(IRssItem, RssItemState)> Accept(IReadOnlyCollection<(IRssItem, RssItemState)> rssItems);
    }
}
