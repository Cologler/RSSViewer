
using RSSViewer.Abstractions;
using RSSViewer.Annotations;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

using Transmission.API.RPC;
using Transmission.API.RPC.Entity;

namespace RSSViewer.Provider.Transmission
{
    internal class TransmissionAcceptHandler : IAcceptHandler
    {
        public string HandlerName => $"Transmission ({this.RpcUrl})";

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

                foreach (var item in rssItems)
                {
                    var ml = item.GetProperty(RssItemProperties.MagnetLink);
                    if (string.IsNullOrWhiteSpace(ml))
                    {
                        return false;
                    }

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
