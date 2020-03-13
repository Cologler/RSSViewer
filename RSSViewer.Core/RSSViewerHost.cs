using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RSSViewer.LocalDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSSViewer
{
    public class RSSViewerHost
    {
        public IServiceProvider ServiceProvider { get; } = BuildServiceProvider();

        public RSSViewerHost()
        {
            using var scope = this.ServiceProvider.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
            ctx.Database.EnsureCreated();
        }

        public RSSViewerSourceProviderManager SourceProviderManager { get; } = new RSSViewerSourceProviderManager();

        public Task SyncAsync()
        {
            return Task.Run(async () =>
            {
                using var scope = this.ServiceProvider.CreateScope();

                var provs = this.SourceProviderManager.GetProviders();
                var ctx = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
                var provMap = ctx.ProviderInfos.ToDictionary(z => z.ProviderName);
                foreach (var prov in provs)
                {
                    var provInfo = provMap.GetValueOrDefault(prov.ProviderName);
                    if (provInfo == null)
                    {
                        provInfo = new ProviderInfo { ProviderName = prov.ProviderName };
                        ctx.Add(provInfo);
                    }

                    var page = await prov.GetItemsListAsync(provInfo.LastSyncId);
                    if (page.LastId is int lastId)
                    {
                        provInfo.LastSyncId = lastId;
                    }

                    var newItems = page.GetItems().Select(z =>
                    {
                        var r = new RssItem();
                        r.UpdateFrom(z);
                        return r;
                    }).ToList();

                    ctx.AddOrIgnoreRange(newItems);
                    ctx.SaveChanges();
                }
            });
        }

        private static IServiceProvider BuildServiceProvider() => CreateServices().BuildServiceProvider();

        private static IServiceCollection CreateServices()
        {
            var sc = new ServiceCollection()
                .AddDbContext<LocalDbContext>(options => options.UseSqlite($"Data Source=localdb.sqlite3"));
            return sc;
        }
    }
}
