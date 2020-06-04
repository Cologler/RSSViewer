
using System;

namespace RSSViewer.Provider.Synology
{
    class SynologyServiceProvider
    {
        public SynologyServiceProvider(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }
    }
}
