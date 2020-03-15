using System.Threading;
using System.Threading.Tasks;

namespace RSSViewer.Abstractions
{
    public interface ISyncSource
    {
        string ProviderName { get; }

        string SyncSourceId { get; }

        ValueTask<ISourceRssItemPage> TryGetItemsAsync(
            int? lastId, CancellationToken cancellationToken, int? limit = null);
    }
}
