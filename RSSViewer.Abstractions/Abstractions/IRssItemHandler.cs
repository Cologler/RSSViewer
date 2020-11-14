using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RSSViewer.Abstractions
{
    public interface IRssItemHandler
    {
        string Id { get; }

        string HandlerName { get; }

        bool CanbeRuleTarget { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rssItems"></param>
        /// <returns>the new state to change</returns>
        IAsyncEnumerable<(IRssItem, RssItemState)> Accept(IReadOnlyCollection<(IRssItem, RssItemState)> rssItems);
    }
}
