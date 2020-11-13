using RSSViewer.Abstractions;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RSSViewer.AcceptHandlers
{
    class ChangeToAcceptedHandler : IAcceptHandler
    {
        public string HandlerName => "Change To Accepted";

        public ValueTask<bool> Accept(IReadOnlyCollection<IRssItem> rssItems) => new ValueTask<bool>(true);
    }
}
