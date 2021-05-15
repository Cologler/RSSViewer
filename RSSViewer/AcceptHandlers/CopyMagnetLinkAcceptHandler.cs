using RSSViewer.Abstractions;
using RSSViewer.Extensions;

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

        public ValueTask HandleAsync(IReadOnlyCollection<IRssItemHandlerContext> contexts)
        {
            var urls = new List<string>();
            var errors = new List<string>();

            foreach (var ctx in contexts)
            {
                var ml = ctx.RssItem.GetPropertyOrDefault(RssItemProperties.MagnetLink);
                if (string.IsNullOrWhiteSpace(ml))
                {
                    errors.Add($"<FeedId={ctx.RssItem.FeedId}, RssId={ctx.RssItem.RssId}>");
                }
                else
                {
                    urls.Add(ml);
                }
            }

            if (errors.Count > 0)
            {
                MessageBox.Show(
                    "Some item's magnet link is empty: " + 
                    Environment.NewLine + 
                    string.Join(Environment.NewLine, errors));
            }

            if (urls.Count > 0)
            {
                var text = string.Join(Environment.NewLine, urls);

                try
                {
                    Clipboard.SetText(text);
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Unable to copy: {e}");
                }
            }
            else
            {
                MessageBox.Show($"Nothing is copied.");
            }

            return ValueTask.CompletedTask;
        }
    }
}
