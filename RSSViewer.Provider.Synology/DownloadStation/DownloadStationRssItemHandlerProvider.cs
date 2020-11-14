
using RSSViewer.Abstractions;
using RSSViewer.DefaultImpls;
using RSSViewer.Utils;

using System;
using System.Collections.Generic;
using System.Text;

namespace RSSViewer.Provider.Synology.DownloadStation
{
    internal class DownloadStationRssItemHandlerProvider : RssItemHandlerProvider<DownloadStationRssItemHandler>
    {
        public DownloadStationRssItemHandlerProvider(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override string ProviderName => "DownloadStation";

        public override IRssItemHandler GetRssItemHandler(string handlerId, Dictionary<string, string> variables)
        {
            var handler = (DownloadStationRssItemHandler) base.GetRssItemHandler(handlerId, variables);
            handler.Id = handlerId;
            return handler;
        }
    }
}
