using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using RSSViewer.Abstractions;

namespace RSSViewer.Services
{
    internal class TrackersService : ITrackersService
    {
        private readonly IViewerLogger _viewerLogger;
        private string[] _trackers = null;

        public TrackersService(IViewerLogger viewerLogger)
        {
            this._viewerLogger = viewerLogger;
        }

        [SupportedOSPlatform("windows")]
        public async ValueTask<string[]> GetExtraTrackersAsync()
        {
            if (this._trackers is null)
            { 
                // the default SocketsHttpHandler has a ton bug, and it cannot open socket on my PC.
                using var httpClient = new HttpClient(new WinHttpHandler());
                string r;
                try
                {
                    r = await httpClient.GetStringAsync("https://raw.githubusercontent.com/ngosang/trackerslist/master/trackers_all_ip.txt");
                }
                catch
                {
                    return Array.Empty<string>();
                }

                var lines = r.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                this._trackers = lines.Where(z => !string.IsNullOrWhiteSpace(z)).ToArray();
                this._viewerLogger.AddLine($"Fetched {this._trackers.Length} trackers.");
            }

            return this._trackers;
        }
    }
}
