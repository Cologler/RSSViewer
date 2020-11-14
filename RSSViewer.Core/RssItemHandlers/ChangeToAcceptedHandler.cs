using RSSViewer.Abstractions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSSViewer.RssItemHandlers
{
    class ChangeToAcceptedHandler : IRssItemHandler
    {
        public string Id => "f385bbb8-6df2-4359-b4b7-4196cce0c4fc";

        public string HandlerName => "Change To Accepted";

        public IAsyncEnumerable<(IRssItem, RssItemState)> Accept(IReadOnlyCollection<(IRssItem, RssItemState)> rssItems)
        {
            return rssItems.Select(z => (z.Item1, RssItemState.Accepted)).ToAsyncEnumerable();
        }
    }
}
