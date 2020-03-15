using RSSViewer.Abstractions;
using System.Collections.Generic;

namespace RSSViewer
{
    public class RSSViewerSourceProviderManager
    {
        private readonly List<ISyncSourceProvider> _sourceProviders = new List<ISyncSourceProvider>();

        public void AddProvider(ISyncSourceProvider sourceProvider)
        {
            this._sourceProviders.Add(sourceProvider);
        }

        public IReadOnlyCollection<ISyncSourceProvider> GetProviders() => this._sourceProviders;
    }
}
