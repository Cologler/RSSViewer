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

                foreach (var (context, url) in rssItemsWithMagnetLink)
                {
                    var ret = await task.CreateAsync(
                               new TaskCreateParameters
                               {
                                   Uri = System.Web.HttpUtility.UrlEncode(url)
                               }).ConfigureAwait(false);
                    if (ret?.Success == true)
                    {
                        context.NewState = RssItemState.Accepted;
                        logger.AddLine($"Sent {context.RssItem.Title} to {this.SiteName}.");
                    }
                }
            }
            catch (HttpRequestException)
            {
                // ignore
            }
        }
    }
}
