using Microsoft.Extensions.DependencyInjection;

using RSSViewer.Abstractions;
using RSSViewer.Configuration;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace RSSViewer.Services
{
    public class AcceptHandlerService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, IAcceptHandlerProvider> _sourceProviders;
        private ImmutableArray<IAcceptHandler> _acceptHandlers;

        public AcceptHandlerService(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
            this._sourceProviders = serviceProvider.GetServices<IAcceptHandlerProvider>().ToDictionary(z => z.ProviderName);

            var configService = serviceProvider.GetRequiredService<ConfigService>();
            configService.OnAppConfChanged += this.Reload;
            this.Reload(configService.AppConf);
        }

        private void Reload(AppConf appConf)
        {
            var dynamicHandlers = appConf.AcceptHandlers.Select(
                z => this._sourceProviders[z.Value.ProviderName].GetAcceptHandler(z.Key, z.Value.Variables))
                .ToArray();

            this._acceptHandlers = this._serviceProvider.GetServices<IAcceptHandler>()
                .Concat(dynamicHandlers)
                .ToImmutableArray();
        }

        public IReadOnlyCollection<IAcceptHandler> GetAcceptHandlers() => this._acceptHandlers;
    }
}
