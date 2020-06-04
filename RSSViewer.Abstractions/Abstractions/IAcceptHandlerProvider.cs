using System.Collections.Generic;

namespace RSSViewer.Abstractions
{
    public interface IAcceptHandlerProvider : IObjectFactoryProvider
    {
        IAcceptHandler GetAcceptHandler(string handlerId, Dictionary<string, string> variables);
    }
}
