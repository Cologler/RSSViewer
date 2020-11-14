
using RSSViewer.Abstractions;
using RSSViewer.DefaultImpls;
using RSSViewer.Utils;

using System;
using System.Collections.Generic;

namespace RSSViewer.Provider.Transmission
{
    internal class TransmissionRssItemHandlerProvider : RssItemHandlerProvider<TransmissionRssItemHandler>
    {
        public TransmissionRssItemHandlerProvider(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override string ProviderName => "Transmission";

        public override IRssItemHandler GetRssItemHandler(string handlerId, Dictionary<string, string> variables)
        {
            var handler = (TransmissionRssItemHandler)base.GetRssItemHandler(handlerId, variables);
            handler.Id = handlerId;
            return handler;
        }
    }
}
