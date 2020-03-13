using RSSViewer.Abstractions;
using System.Collections.Generic;

namespace RSSViewer
{
    public class RSSViewerSourceProviderManager
    {
        private readonly List<ISourceProvider> _sourceProviders = new List<ISourceProvider>();

        public void AddProvider(ISourceProvider sourceProvider)
        {
            this._sourceProviders.Add(sourceProvider);
        }

        public IReadOnlyCollection<ISourceProvider> GetProviders() => this._sourceProviders;
    }
}
