using Microsoft.Extensions.DependencyInjection;

using RSSViewer.Abstractions;
using RSSViewer.Utils;

using System;
using System.Collections.Generic;

namespace RSSViewer.DefaultImpls
{
    public abstract class AcceptHandlerProvider<T> : ObjectFactoryProvider<T>, IAcceptHandlerProvider
        where T : IRssItemHandler
    {
        public IServiceProvider ServiceProvider { get; }

        public AcceptHandlerProvider(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public virtual IRssItemHandler GetAcceptHandler(string handlerId, Dictionary<string, string> variables)
        {
            var service = this.ServiceProvider.GetRequiredService<T>();
            VariablesHelper.Inject(service, VariableInfos, variables);
            return service;
        }
    }
}
