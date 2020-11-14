using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using RSSViewer.Abstractions;
using RSSViewer.Provider.Synology.DownloadStation;

using Synology;

using System.Net.Http;

namespace RSSViewer.Provider.Synology
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSynologyProvider(this IServiceCollection services)
        {
            var synoServiceProvider = new ServiceCollection()
                .AddLogging() // required
                .AddSynology(b =>
                {
                    b.AddApi();
                    b.AddDownloadStation();
                    b.AddDownloadStation2();
                }).AddScoped(provider =>
                {
                    var handler = new HttpClientHandler();
                    handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                    return handler;
                })
                .BuildServiceProvider();

            services.AddSingleton(new SynologyServiceProvider(synoServiceProvider))
                .AddSingleton<IRssItemHandlerProvider, DownloadStationRssItemHandlerProvider>()
                .AddTransient<DownloadStationRssItemHandler>();

            return services;
        }
    }
}
