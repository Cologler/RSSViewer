using Microsoft.Extensions.DependencyInjection;
using RSSViewer.LocalDb;
using RSSViewer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSSViewer.Services
{
    public class SyncService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly NPTask _task;

        public SyncService(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
            this._task = new ExpirableNPTask(TimeSpan.FromMinutes(10), this.SyncCore);
        }

        public Task SyncAsync() => this._task.RunAsync();

        private async Task SyncCore()
        {
            using var scope = this._serviceProvider.CreateScope();

            var provs = scope.ServiceProvider.GetRequiredService<RSSViewerSourceProviderManager>().GetProviders();
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
        }
    }
}
