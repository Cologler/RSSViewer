using System.Threading;

namespace RSSViewer.Helpers.Loaders
{
    public interface IScopedLoader<T>
    {
        T Load(CancellationToken token = default);
    }
}
