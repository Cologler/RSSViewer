using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using RSSViewer.Abstractions;
using RSSViewer.HttpCacheDb;
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
using System.Linq;
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

            this.ServiceProvider.GetRequiredService<AppDirService>().EnsureCreated();
            _ = this.ServiceProvider.GetRequiredService<ITrackersService>().GetExtraTrackersAsync();

            using var scope = this.ServiceProvider.CreateScope();

            scope.ServiceProvider.GetRequiredService<LocalDbContext>()
                .Database.Migrate();

            scope.ServiceProvider.GetRequiredService<RulesDbContext>()
                .Database.Migrate();

            scope.ServiceProvider.GetRequiredService<HttpCacheDbContext>()
                .Database.Migrate();

            var runRulesService = scope.ServiceProvider.GetRequiredService<RunRulesService>();
            this.ServiceProvider.AddListener(EventNames.AddedRssItems, runRulesService.RunForAddedRssItem);
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
            var appDirService = new AppDirService();

            var sc = new ServiceCollection()
                .AddSingleton(appDirService)
                .AddSingleton<JsonService>()
                .AddSingleton<SyncSourceManager>()
                .AddSingleton<RssItemsQueryService>()
                .AddSingleton<RssItemsOperationService>()
                .AddSingleton<HttpService>()
                .AddSingleton<SyncService>()
                .AddSingleton<RssItemHandlersService>()
                .AddSingleton<ITrackersService, TrackersService>()
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
                .AddDbContext<HttpCacheDbContext>((prov, options) => {
                    var path = prov.GetRequiredService<AppDirService>().GetDataFileFullPath("httpcache.sqlite3");
                    options.UseSqlite($"Data Source={path}");
                })
                .AddTransient<IKeywordsFinder, TitleKeywordsFinder>()
                .AddTransient<IKeywordsFinder, MagnetLinkKeywordsFinder>()
                .AddTransient<StringMatcherFactory>()
                .AddRssFetcher()
                .AddSynologyProvider()
                .AddTransmissionProvider()
                .AddSingleton<IViewerLogger, NoneViewerLogger>()
                .AddSingleton(typeof(EventEmitter<>))
                .AddSingleton<RegexCache>()
                ;

            var rssItemStates = new[]
            {
                // ensure sorted:
                RssItemState.Accepted, 
                RssItemState.Rejected, 
                RssItemState.Archived, 
                RssItemState.Undecided
            };

            foreach (var state in rssItemStates)
            {
                sc.AddSingleton<IRssItemHandler>(new ChangeStateHandler(state));
            }
            foreach (var state in rssItemStates.Where(z => z != RssItemState.Undecided))
            {
                sc.AddSingleton<IRssItemHandler>(new ChangeUndecidedStateHandler(state));
            }

            sc.AddSingleton<JsonConverter, TimeSpanConverter>();

            return sc;
        }
    }
}
