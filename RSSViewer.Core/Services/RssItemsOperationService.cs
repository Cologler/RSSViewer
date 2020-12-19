using Microsoft.Extensions.DependencyInjection;

using RSSViewer.Abstractions;
using RSSViewer.LocalDb;
using RSSViewer.Utils;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace RSSViewer.Services
{
    public class RssItemsOperationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly List<OperationsSession> _operationsSessions = new();
        private readonly object _syncRoot = new();

        public RssItemsOperationService(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }

        public Task UndoAsync()
        {
            OperationsSession operationsSession;

            lock (this._syncRoot)
            {
                if (this._operationsSessions.Count == 0)
                    return Task.CompletedTask;

                operationsSession = this._operationsSessions[^1];
                this._operationsSessions.RemoveAt(this._operationsSessions.Count - 1);
            }

            return Task.Run(() => operationsSession.Undo());
        }

        public OperationsSession CreateOperationSession(bool allowUndo)
        {
            var session = new OperationsSession(this._serviceProvider);
            if (allowUndo)
            {
                lock (this._syncRoot)
                {
                    this._operationsSessions.Add(session);
                }
            } 
            return session;
        }

        public class OperationsSession
        {
            private IServiceProvider _serviceProvider;

            private List<ChangeStateOperation> ChangeStateOperations { get; } = new();

            public OperationsSession(IServiceProvider serviceProvider)
            {
                this._serviceProvider = serviceProvider;
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
                    this.ChangeStateOperations.Add(new ChangeStateOperation
                    {
                        FeedId = item.FeedId,
                        RssId = item.RssId,
                        OldState = item.State,
                        NewState = state,
                    });
                    item.State = state;
                }
                ctx.SaveChanges();

                this._serviceProvider.EmitEvent(EventNames.RssItemsStateChanged,
                    this,
                    items.Select(z => ((IRssItem)z, state)).ToList());
            }

            internal void Undo()
            {
                using var scope = this._serviceProvider.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
                var items = new List<RssItem>();
                foreach (var operation in this.ChangeStateOperations)
                {
                    var item = ctx.RssItems.Find(operation.FeedId, operation.RssId);
                    if (item is not null)
                    {
                        item.State = operation.OldState;
                        items.Add(item);
                    }
                }
                ctx.SaveChanges();

                this._serviceProvider.EmitEvent(EventNames.RssItemsStateChanged,
                    this,
                    items.Select(z => ((IRssItem)z, z.State)).ToList());
            }

            public Task AcceptAsync(IReadOnlyCollection<RssItem> items) => this.ChangeStateAsync(items, RssItemState.Accepted);

            public Task RejectAsync(IReadOnlyCollection<RssItem> items) => this.ChangeStateAsync(items, RssItemState.Rejected);

            private Task ChangeStateAsync(IReadOnlyCollection<RssItem> items, RssItemState state)
            {
                if (items is null)
                    throw new ArgumentNullException(nameof(items));

                return Task.Run(() => this.ChangeState(items, state));
            }
        }

        private struct ChangeStateOperation
        {
            public string FeedId;
            public string RssId;
            public RssItemState OldState;
            public RssItemState NewState;
        }
    }
}
