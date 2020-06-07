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

namespace RSSViewer.Provider.Synology.DownloadStation
{
    internal class DownloadStationAcceptHandler : IAcceptHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public string HandlerName => $"DownloadStation ({this.Host}:{this.Port})";

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

        public async ValueTask<bool> Accept(IReadOnlyCollection<IRssItem> rssItems)
        {
            var urls = new List<string>();
            foreach (var item in rssItems)
            {
                var ml = item.GetProperty(RssItemProperties.MagnetLink);
                if (string.IsNullOrWhiteSpace(ml))
                {
                    return false;
                }
                urls.Add(ml);
            }

            if (urls.Count == 0)
            {
                return true;
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

                foreach (var url in urls)
                {
                    var ret = await task.CreateAsync(
                               new TaskCreateParameters
                               {
                                   Uri = url
                               });
                    if (ret?.Success != true)
                    {
                        return false;
                    }
                }
            }
            catch (HttpRequestException)
            {
                return false;
            }

            return true;
        }
    }
}
