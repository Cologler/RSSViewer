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

namespace RSSViewer.Provider.Synology.DownloadStation
{
    internal class DownloadStationAcceptHandler : IRssItemHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public string HandlerName => $"Send To DownloadStation ({this.Host}:{this.Port})";

        public DownloadStationAcceptHandler(SynologyServiceProvider synologyServiceProvider)
        {
            this._serviceProvider = synologyServiceProvider.ServiceProvider;
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

        public async IAsyncEnumerable<(IRssItem, RssItemState)> Accept(IReadOnlyCollection<(IRssItem, RssItemState)> rssItems)
        {
            if (rssItems is null)
                throw new ArgumentNullException(nameof(rssItems));

            var rssItemsWithMagnetLink = rssItems
                .Select(z => z.Item1)
                .Where(z => !string.IsNullOrWhiteSpace(z.GetProperty(RssItemProperties.MagnetLink)))
                .ToList();

            if (rssItemsWithMagnetLink.Count == 0)
            {
                yield break;
            }

            using var scope = this._serviceProvider.CreateScope();

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

            var accepted = new List<IRssItem>();
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

                foreach (var rssItem in rssItemsWithMagnetLink)
                {
                    var url = rssItem.GetProperty(RssItemProperties.MagnetLink);
                    var ret = await task.CreateAsync(
                               new TaskCreateParameters
                               {
                                   Uri = System.Web.HttpUtility.UrlEncode(url)
                               }).ConfigureAwait(false);
                    if (ret?.Success == true)
                    {
                        accepted.Add(rssItem);
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
