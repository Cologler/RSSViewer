using Microsoft.Extensions.DependencyInjection;
using RSSViewer.Abstractions;

namespace RSSViewer.Provider.RssFetcher
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRssFetcher(this IServiceCollection services)
        {
            return services.AddSingleton<ISyncSourceProvider, RssFetcherSyncSourceProvider>();
        }
    }
}
