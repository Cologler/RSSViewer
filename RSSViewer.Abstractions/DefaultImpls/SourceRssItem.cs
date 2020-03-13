using RSSViewer.Abstractions;
using RSSViewer.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace RSSViewer.DefaultImpls
{
    public class SourceRssItem : ISourceRssItem
    {
        private static RssItemProperties[] AllProperties = (RssItemProperties[])Enum.GetValues(typeof(RssItemProperties));
        private readonly Dictionary<RssItemProperties, string> _properties = new Dictionary<RssItemProperties, string>();

        public SourceRssItem(string feedId, string rssId, string rawText)
        {
            this.FeedId = feedId;
            this.RssId = rssId;
            this.RawText = rawText;
        }

        public string FeedId { get; }

        public string RssId { get; }

        public string RawText { get; }

        public string GetProperty(RssItemProperties property)
        {
            return this._properties.GetValueOrDefault(property, string.Empty);
        }

        public void LoadFrom(RssItemXmlReader reader)
        {
            foreach (var property in AllProperties)
            {
                this._properties[property] = reader.Read(property);
            }
        }
    }
}
