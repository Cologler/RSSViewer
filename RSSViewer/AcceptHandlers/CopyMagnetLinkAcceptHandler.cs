using RSSViewer.Abstractions;
using RSSViewer.LocalDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RSSViewer.AcceptHandlers
{
    class CopyMagnetLinkAcceptHandler : IAcceptHandler
    {
        public string HandlerName => "Copy Magnet Link";

        public ValueTask<bool> Accept(IReadOnlyCollection<IRssItem> rssItems)
        {
            var urls = new List<string>();
            foreach (var item in rssItems)
            {
                var ml = item.GetProperty(RssItemProperties.MagnetLink);
                if (string.IsNullOrWhiteSpace(ml))
                {
                    MessageBox.Show($"Some item's magnet link is empty: (FeedId={item.FeedId}, RssId={item.RssId})");
                    return new ValueTask<bool>(false);
                }
                urls.Add(ml);
            }

            var text = string.Join("\r\n", urls);

            try
            {
                Clipboard.SetText(text);
                return new ValueTask<bool>(true);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Unable to copy: {e}");
                return new ValueTask<bool>(false);
            }
        }
    }
}
