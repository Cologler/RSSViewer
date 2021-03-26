using Microsoft.Extensions.DependencyInjection;
using RSSViewer.Abstractions;
using RSSViewer.AcceptHandlers;
using RSSViewer.Provider.RssFetcher;
using RSSViewer.ViewModels;
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

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var sc = RSSViewerHost.CreateServices()
                .AddTransient<IRssItemHandler, CopyMagnetLinkAcceptHandler>()
                .AddSingleton<ViewerLoggerViewModel>()
                .AddSingleton<IViewerLogger>(p => p.GetRequiredService<ViewerLoggerViewModel>())
                .AddAutoMapper(typeof(App).Assembly);
            RSSViewerHost = new RSSViewerHost(sc);
        }
    }
}
