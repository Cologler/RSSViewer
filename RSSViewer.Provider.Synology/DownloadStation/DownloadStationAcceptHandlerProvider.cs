
using RSSViewer.Abstractions;
using RSSViewer.DefaultImpls;
using RSSViewer.Utils;

using System;
using System.Collections.Generic;
using System.Text;

namespace RSSViewer.Provider.Synology.DownloadStation
{
    internal class DownloadStationAcceptHandlerProvider : AcceptHandlerProvider<DownloadStationAcceptHandler>
    {
        public DownloadStationAcceptHandlerProvider(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override string ProviderName => "DownloadStation";
    }
}
