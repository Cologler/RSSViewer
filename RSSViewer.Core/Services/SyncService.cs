using Microsoft.Extensions.DependencyInjection;
using RSSViewer.LocalDb;
using RSSViewer.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RSSViewer.Services
{
    public class SyncService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly NPTask _task;

        public event Action OnSynced;

        public SyncService(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
            this._task = new ExpirableNPTask(TimeSpan.FromMinutes(10), this.SyncCore);
        }

        public Task SyncAsync() => this._task.RunAsync();

        private async Task SyncCore()
        {
            using (var scope = this._serviceProvider.CreateScope())
            {
                var sources = scope.ServiceProvider.GetRequiredService<SyncSourceManager>().GetSyncSources();
                var ctx = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
                var provMap = ctx.SyncSourceInfos.ToDictionary(z => z.SyncSourceId);
                foreach (var source in sources)
                {
                    var provInfo = provMap.GetValueOrDefault(source.SyncSourceId);
                    if (provInfo == null)
                    {
                        provInfo = new SyncSourceInfo { SyncSourceId = source.SyncSourceId };
                        ctx.Add(provInfo);
                    }

                    var page = await source.TryGetItemsAsync(provInfo.LastSyncId, CancellationToken.None);
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

            this.OnSynced?.Invoke();
        }
    }
}
