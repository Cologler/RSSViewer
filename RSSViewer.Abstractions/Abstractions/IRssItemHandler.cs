using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSSViewer.Models;

namespace RSSViewer.Abstractions
{
    public interface IRssItemHandler
    {
        string Id { get; }

        string HandlerName { get; }

        bool CanbeRuleTarget { get; }

        ValueTask HandleAsync(IReadOnlyCollection<IRssItemHandlerContext> contexts);

        string ShortDescription => this.HandlerName;
    }
}
