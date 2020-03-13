using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RSSViewer.Abstractions
{
    public interface ISourceProvider
    {
        string ProviderName { get; }

        IReadOnlyCollection<VariableInfo> GetVariableInfos();

        ValueTask<bool> InitializeAsync(Dictionary<string, object> variables);

        ValueTask<ISourceRssItemPage> GetItemsListAsync(int? lastId = null, int? limit = null);
    }
}
