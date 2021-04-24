using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using RSSViewer.Helpers.Loaders;

namespace RSSViewer.Extensions
{
    public static class Class1
    {
        public static T Load<T>(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            if (serviceProvider is null)
                throw new ArgumentNullException(nameof(serviceProvider));

            return serviceProvider.GetRequiredService<ILoader<T>>().Load(cancellationToken);
        }

        public static IEnumerable<T> LoadMany<T>(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            return Load<IEnumerable<T>>(serviceProvider, cancellationToken);
        }
    }
}
