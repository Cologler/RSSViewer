using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSSViewer.Abstractions;

namespace RSSViewer.Extensions
{
    public static class RssItemExtensions
    {
        public static (string, string) GetKey(this IPartialRssItem rssItem) => (rssItem.FeedId, rssItem.RssId);

        public static string GetPropertyOrDefault(this IPartialRssItem rssItem, RssItemProperties property, string @default = default)
        {
            return rssItem.TryGetProperty(property, out var value) ? value : @default;
        }
    }
}
