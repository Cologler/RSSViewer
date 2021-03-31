
using Microsoft.Extensions.DependencyInjection;

using RSSViewer.Abstractions;
using RSSViewer.Annotations;
using RSSViewer.Extensions;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Transmission.API.RPC;
using Transmission.API.RPC.Entity;

namespace RSSViewer.Provider.Transmission
{
    internal class TransmissionRssItemHandler : IRssItemHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public TransmissionRssItemHandler(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }

        public string Id { get; set; }

        string SiteName => $"Transmission ({this.RpcUrl})";

        public string HandlerName => $"Send To {SiteName}";

        [UserVariable, Required]
        public string RpcUrl { get; set; }

        [UserVariable]
        public string UserName { get; set; }

        [UserVariable]
        public string Password { get; set; }

        public bool CanbeRuleTarget => true;

        public async IAsyncEnumerable<(IPartialRssItem, RssItemState)> HandleAsync(IReadOnlyCollection<(IPartialRssItem, RssItemState)> rssItems)
        {
            if (rssItems is null)
                throw new ArgumentNullException(nameof(rssItems));

            var logger = this._serviceProvider.GetRequiredService<IViewerLogger>();

            var rssItemsWithMagnetLink = rssItems
                .Select(z => z.Item1)
                .Select(z => (RssItem: z, MagnetLink: z.GetPropertyOrDefault(RssItemProperties.MagnetLink)))
                .Where(z =>
                {
                    if (string.IsNullOrWhiteSpace(z.MagnetLink))
                    {
                        logger.AddLine($"Ignore {z.RssItem.Title} which did't have magnet link.");
                        return false;
                    }
                    return true;
                })
                .ToList();

            if (rssItemsWithMagnetLink.Count == 0)
            {
                yield break;
            }

            var options = this._serviceProvider.GetRequiredService<IAddMagnetOptions>();
            int? queuePosition = (await options.IsAddMagnetToQueueTopAsync().ConfigureAwait(false)) ? 0 : null;
            var trackers = await options.GetExtraTrackersAsync().ConfigureAwait(false);

            var task = await Task.Run(() =>
            {
                var accepted = new List<IPartialRssItem>();
                var ids = new List<int>();

                var client = new Client(
                    this.RpcUrl, 
                    Guid.NewGuid().ToString(),
                    this.UserName,
                    this.Password);

                foreach (var (rssItem, ml) in rssItemsWithMagnetLink)
                {
                    var torrent = new NewTorrent
                    {
                        Filename = ml,
                        Paused = false
                    };

                    var newTorrentInfo = client.TorrentAdd(torrent);
                    if (newTorrentInfo != null && newTorrentInfo.ID != 0)
                    {
                        ids.Add(newTorrentInfo.ID);
                        accepted.Add(rssItem);
                        logger.AddLine($"Sent <{rssItem.Title}> to {SiteName}.");
                    }
                }

                if (trackers.Length > 0 && ids.Count > 0)
                {
                    Task.Run(() =>
                    {
                        var retry = 3;
                        while (--retry > 0) // 3 times
                        {
                            try
                            {
                                client.TorrentSet(new()
                                {
                                    IDs = ids.Cast<object>().ToArray(),
                                    TrackerAdd = trackers, // add here for prevent magnet link too long.
                                    QueuePosition = queuePosition
                                });
                                return;
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine(e.ToString());
                            }
                        }
                    });                    
                }

                return accepted;
            }).ConfigureAwait(false);

            foreach (var rssItem in task)
            {
                yield return (rssItem, RssItemState.Accepted);
            }
        }
    }
}
