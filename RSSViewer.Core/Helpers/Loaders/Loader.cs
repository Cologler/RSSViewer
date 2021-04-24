using System;
using System.Threading;

using Microsoft.Extensions.DependencyInjection;

namespace RSSViewer.Helpers.Loaders
{
    class Loader<T> : ILoader<T>
    {
        private readonly IServiceProvider _serviceProvider;

        public Loader(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }

        public T Load(CancellationToken token = default)
        {
            using var scope = this._serviceProvider.CreateScope();
            var loader = scope.ServiceProvider.GetRequiredService<IScopedLoader<T>>();
            return loader.Load(token);
        }
    }
}
