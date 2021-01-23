using System;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

using RSSViewer.Abstractions;
using RSSViewer.HttpCacheDb;

namespace RSSViewer.Services
{
    internal class TrackersService : ITrackersService
    {
        private readonly HttpService _httpService;
        private readonly IViewerLogger _viewerLogger;
        private string[] _trackers = null;

        public TrackersService(HttpService httpService, IViewerLogger viewerLogger)
        {
            this._httpService = httpService;
            this._viewerLogger = viewerLogger;
        }

        [SupportedOSPlatform("windows")]
        public async ValueTask<string[]> GetExtraTrackersAsync()
        {
            if (this._trackers is null)
            {
                var r = await this._httpService
                    .TryGetStringAsync("https://raw.githubusercontent.com/ngosang/trackerslist/master/trackers_all_ip.txt", true, CancellationToken.None);

                if (r is null)
                {
                    return Array.Empty<string>();
                }

                var lines = r.Value.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                this._trackers = lines.Where(z => !string.IsNullOrWhiteSpace(z)).ToArray();

                if (!r.FromCache)
                {
                    this._viewerLogger.AddLine($"Fetched {this._trackers.Length} trackers.");
                }
                else
                {
                    this._viewerLogger.AddLine($"Fetched {this._trackers.Length} trackers (from cache).");
                } 
            }

            return this._trackers;
        }
    }
}
