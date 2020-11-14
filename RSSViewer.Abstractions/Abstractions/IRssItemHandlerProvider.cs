using System.Collections.Generic;

namespace RSSViewer.Abstractions
{
    public interface IRssItemHandlerProvider : IObjectFactoryProvider
    {
        IRssItemHandler GetRssItemHandler(string handlerId, Dictionary<string, string> variables);
    }
}
