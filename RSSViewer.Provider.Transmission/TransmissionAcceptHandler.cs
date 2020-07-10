
using RSSViewer.Abstractions;
using RSSViewer.Annotations;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

using Transmission.API.RPC;
using Transmission.API.RPC.Entity;

namespace RSSViewer.Provider.Transmission
{
    internal class TransmissionAcceptHandler : IAcceptHandler
    {
        public string HandlerName => $"Send To Transmission ({this.RpcUrl})";

        [UserVariable, Required]
        public string RpcUrl { get; set; }

        [UserVariable]
        public string UserName { get; set; }

        [UserVariable]
        public string Password { get; set; }

        public ValueTask<bool> Accept(IReadOnlyCollection<IRssItem> rssItems)
        {
            if (rssItems is null)
                throw new ArgumentNullException(nameof(rssItems));

            return new ValueTask<bool>(Task.Run(() =>
            {
                var client = new Client(
                    this.RpcUrl, 
                    Guid.NewGuid().ToString(),
                    this.UserName,
                    this.Password);

                var magnetLinks = rssItems
                    .Select(z => z.GetProperty(RssItemProperties.MagnetLink))
                    .ToArray();

                if (magnetLinks.Any(string.IsNullOrWhiteSpace))
                {
                    return false;
                }

                foreach (var ml in magnetLinks)
                {
                    var torrent = new NewTorrent
                    {
                        Filename = ml,
                        Paused = false
                    };

                    var newTorrentInfo = client.TorrentAdd(torrent);
                    if (newTorrentInfo == null || newTorrentInfo.ID == 0)
                    {
                        return false;
                    }
                }
                
                return true;
            }));
        }
    }
}
