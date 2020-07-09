using RSSViewer.Abstractions;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RSSViewer.AcceptHandlers
{
    class DoNothingAcceptHandler : IAcceptHandler
    {
        public string HandlerName => "Change Status Only";

        public ValueTask<bool> Accept(IReadOnlyCollection<IRssItem> rssItems) => new ValueTask<bool>(true);
    }
}
