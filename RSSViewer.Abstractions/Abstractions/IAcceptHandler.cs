using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RSSViewer.Abstractions
{
    public interface IAcceptHandler
    {
        string HandlerName { get; }

        ValueTask<bool> Accept(IReadOnlyCollection<IRssItem> rssItems);
    }
}
