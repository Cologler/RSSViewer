using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using RSSViewer.Abstractions;
using RSSViewer.Filter;
using RSSViewer.Helpers;
using RSSViewer.HttpCacheDb;
using RSSViewer.Json;
using RSSViewer.KeywordsFinders;
using RSSViewer.LocalDb;
using RSSViewer.LocalDb.Helpers;
using RSSViewer.Provider.RssFetcher;
using RSSViewer.Provider.Synology;
using RSSViewer.Provider.Transmission;
using RSSViewer.RssItemHandlers;
using RSSViewer.RulesDb;
using RSSViewer.Services;
using RSSViewer.StringMatchers;
using RSSViewer.Utils;

using System;
using System.Collections;
using System.Collections.Generic;
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
#pragma warning disable CA1416
#pragma warning disable CA2012 // 正确使用 ValueTask
            _ = this.ServiceProvider.GetRequiredService<TrackersService>().GetExtraTrackersAsync();
#pragma warning restore CA2012 // 正确使用 ValueTask
#pragma warning restore CA1416

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

        public static IServiceCollection CreateServices()
        {
            var appDirService = new AppDirService();

            var sc = new ServiceCollection()
                .AddSingleton(appDirService)
                .AddSingleton<JsonService>()
                .AddSingleton<SyncSourceManager>()
                .AddSingleton<RssItemsQueryService>()
                .AddSingleton<HttpService>()
                .AddSingleton<SyncService>()
                .AddSingleton<RssItemHandlersService>()
                .AddSingleton<TrackersService>()
                .AddSingleton<RunRulesService>()
                .AddSingleton<ConfigService>()
                .AddSingleton<ClassifyService>()
                .AddSingleton<KeywordsService>()
                .AddSingleton<UndoService>()
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
                .AddTransient<RssItemFilterFactory>()
                .AddRssFetcher()
                .AddSynologyProvider()
                .AddTransmissionProvider()
                .AddSingleton<IViewerLogger, NoneViewerLogger>()
                .AddSingleton(typeof(EventEmitter<>))
                .AddSingleton<RegexCache>()
                .AddSingleton<IRssItemHandler, EmptyHandler>()
                .AddTransient<IAddMagnetOptions, AddMagnetOptions>()
                .AddLogging(cfg => cfg.AddDebug())

                // helpers
                .AddTransient<RssItemsStateChanger>()

                // loader
                .AddScoped<ILoader<IEnumerable<Tag>>>(z => z.GetRequiredService<RulesDbContext>())
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
