﻿
using Microsoft.Extensions.DependencyInjection;

using RSSViewer.Abstractions;
using RSSViewer.Annotations;

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
        private readonly ITrackersService _trackersService;

        public TransmissionRssItemHandler(IServiceProvider serviceProvider, ITrackersService trackersService)
        {
            this._serviceProvider = serviceProvider;
            this._trackersService = trackersService;
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

        public async IAsyncEnumerable<(IRssItem, RssItemState)> Accept(IReadOnlyCollection<(IRssItem, RssItemState)> rssItems)
        {
            if (rssItems is null)
                throw new ArgumentNullException(nameof(rssItems));

            var logger = this._serviceProvider.GetRequiredService<IViewerLogger>();

            var rssItemsWithMagnetLink = rssItems
                .Select(z => z.Item1)
                .Where(z =>
                {
                    if (!string.IsNullOrWhiteSpace(z.GetProperty(RssItemProperties.MagnetLink)))
                    {
                        return true;
                    }

                    logger.AddLine($"Ignore {z.Title} which did't have magnet link.");
                    return false;
                })
                .ToList();

            if (rssItemsWithMagnetLink.Count == 0)
            {
                yield break;
            }

            var trackers = await this._trackersService.GetExtraTrackersAsync();

            var task = await Task.Run(() =>
            {
                var accepted = new List<IRssItem>();
                var ids = new List<int>();

                var client = new Client(
                    this.RpcUrl, 
                    Guid.NewGuid().ToString(),
                    this.UserName,
                    this.Password);

                foreach (var rssItem in rssItemsWithMagnetLink)
                {
                    var ml = rssItem.GetProperty(RssItemProperties.MagnetLink);
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

                if (trackers.Length > 0)
                {
                    try
                    {
                        client.TorrentSet(new()
                        {
                            IDs = ids.Cast<object>().ToArray(),
                            TrackerAdd = trackers
                        });
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                    }
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
