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
        protected async override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var prov = new RssFetcherSourceProvider();
            await prov.InitializeAsync(new Dictionary<string, object>
            {
                [RssFetcherSourceProvider.VarDatabase.VariableName] = @"C:\Users\skyof\Downloads\rss.sqlite3"
            });;

            var host = new RSSViewerHost();
            host.SourceProviderManager.AddProvider(prov);
            await host.SyncAsync();
        }
    }
}
