using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RSSViewer.LocalDb;
using RSSViewer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

            scope.ServiceProvider.GetRequiredService<AppDirService>().EnsureCreated();

            var ctx = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
            ctx.Database.EnsureCreated();
        }

        public RSSViewerSourceProviderManager SourceProviderManager =>
            this.ServiceProvider.GetRequiredService<RSSViewerSourceProviderManager>();

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
                .AddSingleton<RSSViewerSourceProviderManager>()
                .AddSingleton<RssItemsQueryService>()
                .AddSingleton<RssItemsOperationService>()
                .AddSingleton<SyncService>()
                .AddSingleton<ConfigService>()
                .AddSingleton<GroupService>()
                .AddDbContext<LocalDbContext>((prov, options) => {
                    var path = prov.GetRequiredService<AppDirService>().GetDataFileFullPath("localdb.sqlite3");
                    options.UseSqlite($"Data Source={path}");
                })
                ;
            return sc;
        }
    }
}
