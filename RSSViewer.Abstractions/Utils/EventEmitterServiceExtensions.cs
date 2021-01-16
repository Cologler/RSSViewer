using System;

using Microsoft.Extensions.DependencyInjection;

namespace RSSViewer.Utils
{
    public static class EventEmitterServiceExtensions
    {
        public static void AddListener<TArgs>(this IServiceProvider serviceProvider, TypedEventName<TArgs> eventName, 
            EventHandler<TArgs> handler)
        {
            if (serviceProvider is null)
                throw new ArgumentNullException(nameof(serviceProvider));
            if (eventName is null)
                throw new ArgumentNullException(nameof(eventName));
            if (handler is null)
                throw new ArgumentNullException(nameof(handler));

            serviceProvider.GetRequiredService<EventEmitter<TArgs>>()
                .AddListener(eventName.EventName, handler);
        }

        public static void RemoveListener<TArgs>(this IServiceProvider serviceProvider, TypedEventName<TArgs> eventName,
            EventHandler<TArgs> handler)
        {
            if (serviceProvider is null)
                throw new ArgumentNullException(nameof(serviceProvider));
            if (eventName is null)
                throw new ArgumentNullException(nameof(eventName));
            if (handler is null)
                throw new ArgumentNullException(nameof(handler));

            serviceProvider.GetRequiredService<EventEmitter<TArgs>>()
                .RemoveListener(eventName.EventName, handler);
        }

        public static void EmitEvent<TArgs>(this IServiceProvider serviceProvider, TypedEventName<TArgs> eventName, 
            object sender, TArgs args)
        {
            if (serviceProvider is null)
                throw new ArgumentNullException(nameof(serviceProvider));
            if (eventName is null)
                throw new ArgumentNullException(nameof(eventName));

            serviceProvider.GetRequiredService<EventEmitter<TArgs>>()
                .Emit(eventName.EventName, sender, args);
        }
    }
}
