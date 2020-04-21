using System;
using System.Text.Json.Serialization;

namespace RSSViewer.Configuration
{
    public class JsonDateTime
    {
        public long Ticks { get; set; }

        [JsonIgnore]
        public DateTime DateTime
        {
            get => new DateTime(this.Ticks, DateTimeKind.Utc);
            set => this.Ticks = value.Ticks;
        }

        public static implicit operator JsonDateTime(DateTime? dateTime)
        {
            if (dateTime == null)
                return null;

            return new JsonDateTime
            {
                DateTime = dateTime.Value.ToUniversalTime()
            };
        }

        public static implicit operator DateTime?(JsonDateTime dateTime) => dateTime?.DateTime;
    }
}
