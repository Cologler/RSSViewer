using RSSViewer.Abstractions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSSViewer.AcceptHandlers
{
    class ChangeToAcceptedHandler : IAcceptHandler
    {
        public string HandlerName => "Change To Accepted";

        public ValueTask<bool> Accept(IReadOnlyCollection<IRssItem> rssItems) => new ValueTask<bool>(true);

        public IAsyncEnumerable<(IRssItem, RssItemState)> Accept(IReadOnlyCollection<(IRssItem, RssItemState)> rssItems)
        {
            return rssItems.Select(z => (z.Item1, RssItemState.Accepted)).ToAsyncEnumerable();
        }
    }
}
