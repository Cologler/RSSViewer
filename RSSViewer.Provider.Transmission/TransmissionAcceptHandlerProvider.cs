
using RSSViewer.Abstractions;
using RSSViewer.DefaultImpls;
using RSSViewer.Utils;

using System;
using System.Collections.Generic;

namespace RSSViewer.Provider.Transmission
{
    internal class TransmissionAcceptHandlerProvider : AcceptHandlerProvider<TransmissionAcceptHandler>
    {
        public TransmissionAcceptHandlerProvider(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override string ProviderName => "Transmission";
    }
}
