using RSSViewer.Abstractions;
using RSSViewer.Extensions;
using RSSViewer.LocalDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RSSViewer.AcceptHandlers
{
    class CopyMagnetLinkAcceptHandler : IRssItemHandler
    {
        public string Id => "4de881db-4229-4bc0-98cf-10c6acdef452";

        public string HandlerName => "Copy Magnet Link";

        public bool CanbeRuleTarget => false;

        public IAsyncEnumerable<(IPartialRssItem, RssItemState)> HandleAsync(IReadOnlyCollection<(IPartialRssItem, RssItemState)> rssItems)
        {
            var urls = new List<string>();
            foreach (var (item, _) in rssItems)
            {
                var ml = item.GetPropertyOrDefault(RssItemProperties.MagnetLink);
                if (string.IsNullOrWhiteSpace(ml))
                {
                    MessageBox.Show($"Some item's magnet link is empty: (FeedId={item.FeedId}, RssId={item.RssId})");
                }
                else
                {
                    urls.Add(ml);
                }
            }

            if (urls.Count > 0)
            {
                var text = string.Join("\r\n", urls);

                try
                {
                    Clipboard.SetText(text);
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Unable to copy: {e}");
                }
            }

            return AsyncEnumerable.Empty<(IPartialRssItem, RssItemState)>();
        }
    }
}
