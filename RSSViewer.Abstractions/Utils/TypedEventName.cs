using System;

namespace RSSViewer.Utils
{
    public class TypedEventName<TArgs>
    {
        public TypedEventName(string eventName)
        {
            if (String.IsNullOrWhiteSpace(eventName))
                throw new ArgumentException("", nameof(eventName));
            this.EventName = eventName;
        }

        public string EventName { get; }
    }
}
