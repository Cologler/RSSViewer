﻿using RSSViewer.Abstractions;
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

        public IAsyncEnumerable<(IRssItem, RssItemState)> Accept(IReadOnlyCollection<(IRssItem, RssItemState)> rssItems)
        {
            var urls = new List<string>();
            foreach (var (item, _) in rssItems)
            {
                var ml = item.GetProperty(RssItemProperties.MagnetLink);
                if (string.IsNullOrWhiteSpace(ml))
                {
                    MessageBox.Show($"Some item's magnet link is empty: (FeedId={item.FeedId}, RssId={item.RssId})");
                }
                urls.Add(ml);
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

            return AsyncEnumerable.Empty<(IRssItem, RssItemState)>();
        }
    }
}
