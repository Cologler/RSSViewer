using System.Collections.Generic;

namespace RSSViewer.Abstractions
{
    public interface IAcceptHandlerProvider : IObjectFactoryProvider
    {
        IRssItemHandler GetAcceptHandler(string handlerId, Dictionary<string, string> variables);
    }
}
