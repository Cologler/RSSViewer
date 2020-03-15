using Microsoft.Extensions.DependencyInjection;
using RSSViewer.Abstractions;
using RSSViewer.Configuration;
using RSSViewer.Services;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace RSSViewer
{
    public class SyncSourceManager
    {
        private readonly Dictionary<string, ISyncSourceProvider> _sourceProviders;
        private ImmutableArray<ISyncSource> _syncSources;
        private readonly IServiceProvider _serviceProvider;

        public SyncSourceManager(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
            this._sourceProviders = serviceProvider.GetServices<ISyncSourceProvider>().ToDictionary(z => z.ProviderName);
            var configService = serviceProvider.GetRequiredService<ConfigService>();
            this.Reload(configService.AppConf);
            configService.OnAppConfChanged += this.Reload;
        }

        void Reload(AppConf conf)
        {
            this._syncSources = conf.SyncSources
                .Select(z => this._sourceProviders[z.Value.ProviderName].GetSyncSource(z.Key, z.Value.Variables))
                .ToImmutableArray();
        }

        public IEnumerable<ISyncSource> GetSyncSources()
        {
            return this._syncSources;
        }
    }
}
