using RSSViewer.Abstractions;
using System.Collections.Generic;

namespace RSSViewer
{
    public class RSSViewerSourceProviderManager
    {
        private readonly List<ISyncSourceProvider> _sourceProviders = new List<ISyncSourceProvider>();
        private readonly List<ISyncSource> _syncSources = new List<ISyncSource>();

        public void AddProvider(ISyncSourceProvider sourceProvider)
        {
            this._sourceProviders.Add(sourceProvider);
        }

        public IReadOnlyCollection<ISyncSourceProvider> GetProviders() => this._sourceProviders;

        public IEnumerable<ISyncSource> GetSyncSources()
        {
            return this._syncSources;
        }

        public void AddSyncSource(ISyncSource source)
        {
            this._syncSources.Add(source);
        }
    }
}
