using RSSViewer.LocalDb;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RSSViewer.AcceptHandlers
{
    public interface IAcceptHandler
    {
        string HandlerName { get; }

        ValueTask<bool> Accept(IReadOnlyCollection<RssItem> rssItems);
    }
}
