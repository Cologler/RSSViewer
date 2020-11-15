using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using RSSViewer.Abstractions;
using RSSViewer.Json;
using RSSViewer.KeywordsFinders;
using RSSViewer.LocalDb;
using RSSViewer.Provider.RssFetcher;
using RSSViewer.Provider.Synology;
using RSSViewer.Provider.Transmission;
using RSSViewer.RssItemHandlers;
using RSSViewer.RulesDb;
using RSSViewer.Services;
using RSSViewer.StringMatchers;
using RSSViewer.Utils;

using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RSSViewer
{
    public class RSSViewerHost
    {
        public IServiceProvider ServiceProvider { get; }

        public RSSViewerHost(IServiceCollection services)
        {
            this.ServiceProvider = services.BuildServiceProvider();

            using var scope = this.ServiceProvider.CreateScope();

            scope.ServiceProvider.GetRequiredService<LocalDbContext>()
                .Database.Migrate();

            scope.ServiceProvider.GetRequiredService<RulesDbContext>()
                .Database.Migrate();

            var runRulesService = scope.ServiceProvider.GetRequiredService<RunRulesService>();
            scope.ServiceProvider.GetRequiredService<AppDirService>().EnsureCreated();
            this.ServiceProvider.AddListener(EventNames.AddedRssItems, runRulesService.RunForAdded);
        }

        public SyncSourceManager SourceProviderManager =>
            this.ServiceProvider.GetRequiredService<SyncSourceManager>();

        public Task SyncAsync()
        {
            return this.ServiceProvider.GetRequiredService<SyncService>().SyncAsync();
        }

        public RssItemsQueryService Query() => this.ServiceProvider.GetRequiredService<RssItemsQueryService>();

        public RssItemsOperationService Modify() => this.ServiceProvider.GetRequiredService<RssItemsOperationService>();

        public static IServiceCollection CreateServices()
        {
            var sc = new ServiceCollection()
                .AddSingleton<AppDirService>()
                .AddSingleton<SyncSourceManager>()
                .AddSingleton<RssItemsQueryService>()
                .AddSingleton<RssItemsOperationService>()
                .AddSingleton<SyncService>()
                .AddSingleton<RssItemHandlersService>()
                .AddSingleton<RunRulesService>()
                .AddSingleton<ConfigService>()
                .AddSingleton<GroupService>()
                .AddSingleton<KeywordsService>()
                .AddDbContext<LocalDbContext>((prov, options) => {
                    var path = prov.GetRequiredService<AppDirService>().GetDataFileFullPath("localdb.sqlite3");
                    options.UseSqlite($"Data Source={path}");
                })
                .AddDbContext<RulesDbContext>((prov, options) => {
                    var path = prov.GetRequiredService<AppDirService>().GetDataFileFullPath("rulesdb.sqlite3");
                    options.UseSqlite($"Data Source={path}");
                })
                .AddTransient<IKeywordsFinder, TitleKeywordsFinder>()
                .AddTransient<IKeywordsFinder, MagnetLinkKeywordsFinder>()
                .AddTransient<StringMatcherFactory>()
                .AddRssFetcher()
                .AddSynologyProvider()
                .AddTransmissionProvider()
                .AddSingleton<IViewerLogger, NoneViewerLogger>()
                .AddSingleton<IRssItemHandler, ChangeToAcceptedHandler>()
                .AddSingleton<IRssItemHandler, ChangeToRejectedHandler>()
                .AddSingleton<IRssItemHandler, ChangeUndecidedToAcceptedHandler>()
                .AddSingleton<IRssItemHandler, ChangeUndecidedToRejectedHandler>()
                .AddSingleton(typeof(EventEmitter<>))
                ;

            sc.AddSingleton<JsonConverter, TimeSpanConverter>();

            return sc;
        }
    }
}
