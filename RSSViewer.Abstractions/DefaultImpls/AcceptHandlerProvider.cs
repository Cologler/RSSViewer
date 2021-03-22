using Microsoft.Extensions.DependencyInjection;

using RSSViewer.Abstractions;
using RSSViewer.Utils;

using System;
using System.Collections.Generic;

namespace RSSViewer.DefaultImpls
{
    public abstract class RssItemHandlerProvider<T> : ObjectFactoryProvider<T>, IRssItemHandlerProvider
        where T : IRssItemHandler
    {
        public IServiceProvider ServiceProvider { get; }

        public RssItemHandlerProvider(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public virtual IRssItemHandler GetRssItemHandler(string handlerId, Dictionary<string, string> variables)
        {
            var service = this.ServiceProvider.GetRequiredService<T>();
            VariablesHelper.Inject(service, VariableInfos, variables);
            return service;
        }
    }
}
