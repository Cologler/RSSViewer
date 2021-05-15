
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

        public async ValueTask HandleAsync(IReadOnlyCollection<IRssItemHandlerContext> contexts)
        {
            if (contexts is null)
                throw new ArgumentNullException(nameof(contexts));

            var logger = this._serviceProvider.GetRequiredService<IViewerLogger>();

            var rssItemsWithMagnetLink = contexts
                .Select(z => (Context: z, MagnetLink: z.RssItem.GetPropertyOrDefault(RssItemProperties.MagnetLink)))
                .Where(z =>
                {
                    if (string.IsNullOrWhiteSpace(z.MagnetLink))
                    {
                        logger.AddLine($"Ignore {z.Context.RssItem.Title} which did't have magnet link.");
                        return false;
                    }
                    return true;
                })
                .ToList();

            if (rssItemsWithMagnetLink.Count == 0)
            {
                return;
            }

            var options = this._serviceProvider.GetRequiredService<IAddMagnetOptions>();
            int? queuePosition = (await options.IsAddMagnetToQueueTopAsync().ConfigureAwait(false)) ? 0 : null;
            var trackers = await options.GetExtraTrackersAsync().ConfigureAwait(false);

            await Task.Run(() =>
            {
                var ids = new List<int>();

                var client = new Client(
                    this.RpcUrl, 
                    Guid.NewGuid().ToString(),
                    this.UserName,
                    this.Password);

                foreach (var (ctx, ml) in rssItemsWithMagnetLink)
                {
                    var torrent = new NewTorrent
                    {
                        Filename = ml,
                        Paused = false
                    };

                    try
                    {
                        var newTorrentInfo = client.TorrentAdd(torrent);
                        if (newTorrentInfo != null && newTorrentInfo.ID != 0)
                        {
                            ids.Add(newTorrentInfo.ID);
                            ctx.NewState = RssItemState.Accepted;
                            logger.AddLine($"Sent <{ctx.RssItem.Title}> to {this.SiteName}.");
                        }
                    } 
                    catch (Exception e)
                    {
                        logger.AddLine($"Error on <{ctx.RssItem.Title}>: {e.Message}.");
                    }
                }

                if (ids.Count > 0 && trackers.Length > 0)
                {
                    // don't wait this...
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
            }).ConfigureAwait(false);
        }
    }
}
