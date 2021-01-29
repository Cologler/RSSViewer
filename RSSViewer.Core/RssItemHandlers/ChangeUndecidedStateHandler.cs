using RSSViewer.Abstractions;

using System;
using System.Collections.Generic;
using System.Linq;

namespace RSSViewer.RssItemHandlers
{
    class ChangeUndecidedStateHandler : IRssItemHandler
    {
        private readonly RssItemState _newState;

        public static string GetId(RssItemState newState)
        {
            return newState switch
            {
                RssItemState.Rejected => "46d73fe1-77c4-446b-82f8-eb1fbae7a3ff",
                RssItemState.Undecided => "3567c0e8-4e29-44e1-8d20-ed50105306a0",
                RssItemState.Accepted => "2e09db60-8ac2-4d29-879a-670a279a9c80",
                RssItemState.Archived => "b2830422-8138-4456-9bac-65872a3266e0",
                _ => throw new NotImplementedException()
            };
        }

        public ChangeUndecidedStateHandler(RssItemState newState)
        {
            this._newState = newState;
            this.Id = GetId(newState);
            this.HandlerName = $"Change Undecided To {newState}";
        }

        public string Id { get; }

        public string HandlerName { get; }

        public bool CanbeRuleTarget => false;

        public IAsyncEnumerable<(IPartialRssItem, RssItemState)> HandleAsync(IReadOnlyCollection<(IPartialRssItem, RssItemState)> rssItems)
        {
            return rssItems
                .Where(z => z.Item2 != this._newState)
                .Where(z => z.Item2 == RssItemState.Undecided)
                .Select(z => (z.Item1, this._newState))
                .ToAsyncEnumerable();
        }
    }
}
