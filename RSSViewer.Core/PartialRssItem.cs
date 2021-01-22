using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSSViewer.Abstractions;

namespace RSSViewer
{
    class PartialRssItem : IPartialRssItem
    {
        public string FeedId { get; set; }

        public string RssId { get; set; }

        public string Title { get; set; }

        public RssItemState State { get; set; }

        public string MagnetLink { get; set; }

        public bool TryGetProperty(RssItemProperties property, out string value)
        {
            switch (property)
            {
                case RssItemProperties.Title:
                    value = this.Title;
                    break;

                case RssItemProperties.MagnetLink:
                    value = this.MagnetLink;
                    break;

                default:
                    value = null;
                    return false;
            }

            return true;
        }
    }
}
