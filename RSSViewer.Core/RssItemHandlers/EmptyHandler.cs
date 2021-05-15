using RSSViewer.Abstractions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RSSViewer.RssItemHandlers
{
    /// <summary>
    /// Do nothing. this is use to group rules.
    /// </summary>
    class EmptyHandler : IRssItemHandler
    {
        public string Id => KnownHandlerIds.EmptyHandlerId;

        public string HandlerName => "Do Nothing";

        public bool CanbeRuleTarget => true;

        public ValueTask HandleAsync(IReadOnlyCollection<IRssItemHandlerContext> contexts) => ValueTask.CompletedTask;
    }
}
