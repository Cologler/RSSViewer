using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RSSViewer.Abstractions
{
    public interface ISyncSourceProvider
    {
        string ProviderName { get; }

        IReadOnlyCollection<VariableInfo> GetVariableInfos();

        ISyncSource GetSyncSource(string syncSourceId, Dictionary<string, string> variables);
    }
}
