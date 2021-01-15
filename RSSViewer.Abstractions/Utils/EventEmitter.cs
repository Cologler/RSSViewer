using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;

namespace RSSViewer.Utils
{
    public class EventEmitter<TArgs>
    {
        private ImmutableDictionary<string, EventHandlersContainer> _events = ImmutableDictionary<string, EventHandlersContainer>.Empty
            .WithComparers(StringComparer.OrdinalIgnoreCase);

        private class EventHandlersContainer
        {
            public event EventHandler<TArgs> Handlers;

            public void Emit(object sender, TArgs args)
            {
                this.Handlers?.Invoke(sender, args);
            }
        }

        private EventHandlersContainer GetEvents(string eventName, bool create)
        {
            if (this._events.TryGetValue(eventName, out var c))
                return c;

            if (!create)
                return null;

            var newContainer = new EventHandlersContainer();

            while (true)
            {
                var events = this._events;
                var newEvents = events.SetItem(eventName, newContainer);
                if (ReferenceEquals(Interlocked.CompareExchange(ref this._events, newEvents, events), events))
                    return newContainer;
                if (this._events.TryGetValue(eventName, out var e))
                    return e;
            }
        }

        public void AddListener(string eventName, EventHandler<TArgs> handler)
        {
            this.GetEvents(eventName, true).Handlers += handler;
        }

        public void Emit(string eventName, object sender, TArgs args)
        {
            this.GetEvents(eventName, false)?.Emit(sender, args);
        }
    }
}
