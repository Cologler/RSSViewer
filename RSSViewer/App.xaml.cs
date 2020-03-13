using Microsoft.Extensions.DependencyInjection;
using RSSViewer.AcceptHandlers;
using RSSViewer.Provider.RssFetcher;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RSSViewer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static RSSViewerHost RSSViewerHost { get; private set; }

        protected async override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var sc = RSSViewerHost.CreateServices()
                .AddTransient<IAcceptHandler, CopyMagnetLinkAcceptHandler>();
            RSSViewerHost = new RSSViewerHost(sc);

            var prov = new RssFetcherSourceProvider();
            await prov.InitializeAsync(new Dictionary<string, object>
            {
                [RssFetcherSourceProvider.VarDatabase.VariableName] = @"C:\Users\skyof\Downloads\rss.sqlite3"
            });;

            RSSViewerHost.SourceProviderManager.AddProvider(prov);
        }
    }
}
