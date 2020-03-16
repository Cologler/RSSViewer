using System.Collections.Generic;

namespace RSSViewer.Abstractions
{
    public interface IObjectFactoryProvider
    {
        string ProviderName { get; }

        IReadOnlyCollection<VariableInfo> GetVariableInfos();
    }
}
