using Microsoft.Extensions.DependencyInjection;
using RSSViewer.LocalDb;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RSSViewer.Services
{
    public class RssItemsOperationService
    {
        private readonly IServiceProvider _serviceProvider;

        public RssItemsOperationService(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }

        public Task AcceptAsync(IReadOnlyCollection<RssItem> items) => this.ChangeStateAsync(items, RssItemState.Accepted);

        public Task RejectAsync(IReadOnlyCollection<RssItem> items) => this.ChangeStateAsync(items, RssItemState.Rejected);

        private Task ChangeStateAsync(IReadOnlyCollection<RssItem> items, RssItemState state)
        {
            if (items is null)
                throw new ArgumentNullException(nameof(items));

            return Task.Run(() => this.ChangeState(items, state));
        }

        internal void ChangeState(IReadOnlyCollection<RssItem> items, RssItemState state)
        {
            if (items is null)
                throw new ArgumentNullException(nameof(items));

            using var scope = this._serviceProvider.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
            ctx.AttachRange(items);
            foreach (var item in items)
            {
                item.State = state;
            }
            ctx.SaveChanges();
        }
    }
}
