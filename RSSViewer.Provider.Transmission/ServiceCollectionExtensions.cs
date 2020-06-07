using Microsoft.Extensions.DependencyInjection;

using RSSViewer.Abstractions;

namespace RSSViewer.Provider.Transmission
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTransmissionProvider(this IServiceCollection services)
        {
            return services.AddSingleton<IAcceptHandlerProvider, TransmissionAcceptHandlerProvider>();
        }
    }
}
