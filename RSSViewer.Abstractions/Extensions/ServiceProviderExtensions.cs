using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using RSSViewer.Abstractions;

namespace RSSViewer.Extensions
{
    public static class ServiceProviderExtensions
    {
        public static T Load<T>(this IServiceProvider serviceProvider, bool newScope, CancellationToken cancellationToken = default)
        {
            if (serviceProvider is null)
                throw new ArgumentNullException(nameof(serviceProvider));

            if (newScope)
            {
                using var scope = serviceProvider.CreateScope();
                return scope.ServiceProvider.GetRequiredService<ILoader<T>>().Load(cancellationToken);
            }
            else
            {
                return serviceProvider.GetRequiredService<ILoader<T>>().Load(cancellationToken);
            }
        }

        /// <summary>
        /// load with new scope.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serviceProvider"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static T Load<T>(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            return Load<T>(serviceProvider, true, cancellationToken);
        }

        public static IEnumerable<T> LoadMany<T>(this IServiceProvider serviceProvider, bool newScope, CancellationToken cancellationToken = default)
        {
            return Load<IEnumerable<T>>(serviceProvider, cancellationToken);
        }

        public static IEnumerable<T> LoadMany<T>(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            return LoadMany<T>(serviceProvider, true, cancellationToken);
        }
    }
}
