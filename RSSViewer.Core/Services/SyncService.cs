using Microsoft.Extensions.DependencyInjection;
using RSSViewer.Abstractions;
using RSSViewer.LocalDb;
using RSSViewer.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RSSViewer.Services
{
    public class SyncService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IViewerLogger _viewerLogger;
        private readonly NPTask _task;

        public event Action OnSynced;

        public SyncService(IServiceProvider serviceProvider, IViewerLogger viewerLogger)
        {
            this._serviceProvider = serviceProvider;
            this._viewerLogger = viewerLogger;
            this._task = new ExpirableNPTask(TimeSpan.FromMinutes(10), this.SyncCore);
        }

        public TimeSpan? LastSyncElapsed { get; private set; }

        public Task SyncAsync() => this._task.RunAsync();

        private async Task SyncCore()
        {
            var sw = Stopwatch.StartNew();
            using (var scope = this._serviceProvider.CreateScope())
            {
                var sources = scope.ServiceProvider.GetRequiredService<SyncSourceManager>().GetSyncSources();
                var ctx = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
                var syncInfoTable = ctx.SyncSourceInfos.ToDictionary(z => z.SyncSourceId);
                foreach (var source in sources)
                {
                    var syncInfo = syncInfoTable.GetValueOrDefault(source.SyncSourceId);
                    await SyncCoreAsync(ctx, source, syncInfo, CancellationToken.None).ConfigureAwait(false);
                }
                ctx.SaveChanges();
            }

            this.OnSynced?.Invoke();
            sw.Stop();
            this.LastSyncElapsed = sw.Elapsed;

            this._viewerLogger.AddLine($"Synced source takes {sw.Elapsed.TotalSeconds}s.");
        }

        public Task SyncAsync(ISyncSource syncSource)
        {
            if (syncSource is null)
                throw new ArgumentNullException(nameof(syncSource));

            return Task.Run(async () =>
            {
                using (var scope = this._serviceProvider.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
                    var syncInfo = ctx.SyncSourceInfos
                        .FirstOrDefault(z => z.SyncSourceId == syncSource.SyncSourceId);
                    await SyncCoreAsync(ctx, syncSource, syncInfo, CancellationToken.None).ConfigureAwait(false);
                    ctx.SaveChanges();
                }

                this.OnSynced?.Invoke();
            });
        }

        private static async Task SyncCoreAsync(LocalDbContext ctx, ISyncSource syncSource, SyncSourceInfo syncInfo, 
            CancellationToken cancellationToken)
        {
            if (syncInfo is null)
            {
                syncInfo = new SyncSourceInfo { SyncSourceId = syncSource.SyncSourceId };
                ctx.Add(syncInfo);
            }

            var page = await syncSource.TryGetItemsAsync(syncInfo.LastSyncId, cancellationToken)
                .ConfigureAwait(false);
            if (page.LastId is int lastId)
            {
                syncInfo.LastSyncId = lastId;
            }

            var newItems = page.GetItems().Select(z =>
            {
                var r = new RssItem();
                r.UpdateFrom(z);
                return r;
            }).ToList();

            ctx.AddOrIgnoreRange(newItems);
        }
    }
}
