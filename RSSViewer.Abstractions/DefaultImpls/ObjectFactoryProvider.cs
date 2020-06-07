
using RSSViewer.Abstractions;
using RSSViewer.Utils;

using System.Collections.Generic;

namespace RSSViewer.DefaultImpls
{
    public abstract class ObjectFactoryProvider<T> : IObjectFactoryProvider
    {
        protected static readonly VariableInfo[] VariableInfos = VariablesHelper.GetVariableInfos(typeof(T));

        public abstract string ProviderName { get; }

        public virtual IReadOnlyCollection<VariableInfo> GetVariableInfos() => VariableInfos;
    }
}
