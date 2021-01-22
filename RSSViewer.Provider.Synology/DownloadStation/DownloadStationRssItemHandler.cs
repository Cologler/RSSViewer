using Microsoft.Extensions.DependencyInjection;

using RSSViewer.Abstractions;

using Synology;
using Synology.Interfaces;
using Synology.DownloadStation.Task.Parameters;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using Synology.Api.Auth.Parameters;
using RSSViewer.Annotations;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using RSSViewer.Extensions;

namespace RSSViewer.Provider.Synology.DownloadStation
{
    internal class DownloadStationRssItemHandler : IRssItemHandler
    {
        public string Id { get; set; }

        private readonly SynologyServiceProvider _synologyServiceProvider;
        private readonly IServiceProvider _serviceProvider;

        string SiteName => $"DownloadStation ({this.Host}:{this.Port})";

        public string HandlerName => $"Send To {SiteName}";

        public DownloadStationRssItemHandler(IServiceProvider serviceProvider, SynologyServiceProvider synologyServiceProvider)
        {
            this._serviceProvider = serviceProvider;
            this._synologyServiceProvider = synologyServiceProvider;
        }

        [UserVariable, Required]
        public string Host { get; set; }

        [UserVariable, Required]
        public string UserName { get; set; }

        [UserVariable, Required]
        public string Password { get; set; }

        [UserVariable, Required]
        public int Port { get; set; }

        [UserVariable, Required]
        public bool IsSsl { get; set; }

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

            using var scope = this._synologyServiceProvider.ServiceProvider.CreateScope();

            var settings = scope.ServiceProvider.GetService<ISynologyConnectionSettings>();

            settings.BaseHost = this.Host;
            settings.Username = this.UserName;
            settings.Password = this.Password;
            settings.Ssl = this.IsSsl;
            if (this.IsSsl)
            {
                settings.SslPort = this.Port;
            }
            else
            {
                settings.Port = this.Port;
            }

            var conn = scope.ServiceProvider.GetService<ISynologyConnection>();

            var accepted = new List<IPartialRssItem>();
            try
            {
                var resp = await conn.Api().Auth().LoginAsync(new LoginParameters
                {
                    Username = this.UserName,
                    Password = this.Password
                });

                var task = conn
                    .DownloadStation()
                    .Task();

                foreach (var (rssItem, url) in rssItemsWithMagnetLink)
                {
                    var ret = await task.CreateAsync(
                               new TaskCreateParameters
                               {
                                   Uri = System.Web.HttpUtility.UrlEncode(url)
                               }).ConfigureAwait(false);
                    if (ret?.Success == true)
                    {
                        accepted.Add(rssItem);
                        logger.AddLine($"Sent {rssItem.Title} to {SiteName}.");
                    }
                }
            }
            catch (HttpRequestException)
            {
                // ignore
            }

            foreach (var rssItem in accepted)
            {
                yield return (rssItem, RssItemState.Accepted);
            }
        }
    }
}
