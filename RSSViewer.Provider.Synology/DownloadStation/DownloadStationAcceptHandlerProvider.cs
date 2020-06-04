
using RSSViewer.Abstractions;
using RSSViewer.Utils;

using System;
using System.Collections.Generic;
using System.Text;

namespace RSSViewer.Provider.Synology.DownloadStation
{
    internal class DownloadStationAcceptHandlerProvider : IAcceptHandlerProvider
    {
        private static readonly VariableInfo[] VariableInfos = VariablesHelper.GetVariableInfos(typeof(DownloadStationAcceptHandler));
        private readonly IServiceProvider _synologyServiceProvider;

        public string ProviderName => "DownloadStation";

        public DownloadStationAcceptHandlerProvider(SynologyServiceProvider synologyServiceProvider)
        {
            this._synologyServiceProvider = synologyServiceProvider.ServiceProvider;
        }

        public IAcceptHandler GetAcceptHandler(string handlerId, Dictionary<string, string> variables)
        {
            var handler = new DownloadStationAcceptHandler(this._synologyServiceProvider);
            VariablesHelper.Inject(handler, VariableInfos, variables);
            return handler;
        }

        public IReadOnlyCollection<VariableInfo> GetVariableInfos() => VariableInfos;
    }
}
