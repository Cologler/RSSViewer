﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using RSSViewer.Abstractions;
using RSSViewer.Helpers;
using RSSViewer.Utils;

namespace RSSViewer.LocalDb.Helpers
{
    public class RssItemsStateChanger<TFrom>
    {
        protected readonly IServiceProvider _serviceProvider;
        private readonly IRssItemFinder<TFrom> _rssItemFinder;

        public RssItemsStateChanger(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
            this._rssItemFinder = serviceProvider.GetRequiredService<IRssItemFinder<TFrom>>();
        }

        /// <summary>
        /// the changes to save.
        /// </summary>
        public List<(TFrom, RssItemStateSnapshot)> Changes { get; } = new();

        /// <summary>
        /// the saved changes;
        /// </summary>
        public List<(TFrom, RssItemState)> Changed { get; } = new();

        public virtual IUndoable SaveChanges()
        {
            if (this.Changes.Count > 0)
            {
                using var scope = this._serviceProvider.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<LocalDbContext>();

                var unsavedChanged = new List<(TFrom, RssItemState)>();
                var oldStates = new List<RssItemOldStateSnapshot>();

                foreach (var (item, state) in this.Changes)
                {
                    var ri = this._rssItemFinder.FindRssItem(ctx, item);
                    if (ri is not null)
                    {
                        var oldState = new RssItemOldStateSnapshot();
                        oldState.UpdateFrom(ri);
                        oldStates.Add(oldState);

                        state.UpdateTo(ri);
                        unsavedChanged.Add((item, state.State));
                    }
                }

                Debug.Assert(unsavedChanged.Count == oldStates.Count);

                if (unsavedChanged.Count > 0)
                {
                    ctx.SaveChanges();
                    this.Changed.AddRange(unsavedChanged);
                    return new Undoable(oldStates);
                }
            }

            return EmptyUndoable.Default;
        }

        class Undoable : IUndoable
        {
            private readonly ICollection<RssItemOldStateSnapshot> _states;

            public Undoable(ICollection<RssItemOldStateSnapshot> states)
            {
                this._states = states ?? throw new ArgumentNullException(nameof(states));
            }

            public void Undo(IServiceProvider serviceProvider)
            {
                var saver = new RssItemsStateChanger<RssItemOldStateSnapshot>(serviceProvider);
                saver.Changes.AddRange(this._states.Select(z => (z, (RssItemStateSnapshot)z)));
                saver.SaveChanges();
            }
        }
    }

    public class RssItemsStateChanger : RssItemsStateChanger<IPartialRssItem>
    {
        public RssItemsStateChanger(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public void AddFromUserAction(IPartialRssItem item, RssItemState newState, IRssItemHandler rssItemHandler)
        {
            this.Changes.Add((item, new RssItemStateSnapshot
            {
                State = newState,
                StateChangeReason = RssItemStateChangeReason.UserChoicedHandler,
                StateChangeReasonExtras = rssItemHandler.Id
            }));
        }

        public void AddFromHandler(IPartialRssItem item, RssItemState newState, IRssItemHandler rssItemHandler)
        {
            this.Changes.Add((item, new RssItemStateSnapshot
            {
                State = newState,
                StateChangeReason = RssItemStateChangeReason.MatchRuleHandler,
                StateChangeReasonExtras = rssItemHandler.Id
            }));
        }

        public override IUndoable SaveChanges()
        {
            var rv = base.SaveChanges();
            this._serviceProvider.EmitEvent(EventNames.RssItemsStateChanged, this, this.Changed.ToList());
            return rv;
        }
    }
}
