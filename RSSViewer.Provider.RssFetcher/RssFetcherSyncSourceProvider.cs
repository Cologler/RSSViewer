using RSSViewer.Abstractions;
using RSSViewer.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace RSSViewer.Provider.RssFetcher
{
    public class RssFetcherSyncSourceProvider : ISyncSourceProvider
    {
        private static readonly VariableInfo[] VariableInfos = VariablesHelper.GetVariableInfos(typeof(RssFetcherSyncSource));

        public string ProviderName => "RssFetcher";

        public IReadOnlyCollection<VariableInfo> GetVariableInfos() => VariableInfos;

        public ISyncSource GetSyncSource(string syncSourceId, Dictionary<string, string> variables)
        {
            var ss = new RssFetcherSyncSource(syncSourceId);
            VariablesHelper.Inject(ss, VariableInfos, variables);
            return ss;
        }
    }
}
