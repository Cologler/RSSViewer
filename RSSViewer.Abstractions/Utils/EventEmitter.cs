using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace RSSViewer.Utils
{
    public class EventEmitter<TArgs>
    {
        private readonly object _syncRoot = new object();
        private ImmutableDictionary<string, EventBox> _boxes = ImmutableDictionary<string, EventBox>.Empty
            .WithComparers(StringComparer.OrdinalIgnoreCase);

        private class EventBox
        {
            public event EventHandler<TArgs> Handlers;

            public void Emit(object sender, TArgs args)
            {
                this.Handlers?.Invoke(sender, args);
            }
        }

        private EventBox GetEventBox(string eventName, bool create)
        {
            if (this._boxes.TryGetValue(eventName, out var eb))
                return eb;

            if (!create)
                return null;

            lock (this._syncRoot)
            {
                if (this._boxes.TryGetValue(eventName, out eb))
                    return eb;

                eb = new EventBox();
                this._boxes = this._boxes.SetItem(eventName, eb);
                return eb;
            }
        }

        public void AddListener(string eventName, EventHandler<TArgs> handler)
        {
            this.GetEventBox(eventName, true).Handlers += handler;
        }

        public void Emit(string eventName, object sender, TArgs args)
        {
            this.GetEventBox(eventName, false)?.Emit(sender, args);
        }
    }
}
