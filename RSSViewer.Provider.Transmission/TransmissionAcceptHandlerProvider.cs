
using RSSViewer.Abstractions;
using RSSViewer.Utils;

using System.Collections.Generic;

namespace RSSViewer.Provider.Transmission
{
    internal class TransmissionAcceptHandlerProvider : IAcceptHandlerProvider
    {
        private static readonly VariableInfo[] VariableInfos = VariablesHelper.GetVariableInfos(typeof(TransmissionAcceptHandler));

        public string ProviderName => "Transmission";

        public IAcceptHandler GetAcceptHandler(string handlerId, Dictionary<string, string> variables)
        {
            var handler = new TransmissionAcceptHandler();
            VariablesHelper.Inject(handler, VariableInfos, variables);
            return handler;
        }

        public IReadOnlyCollection<VariableInfo> GetVariableInfos() => VariableInfos;
    }
}
