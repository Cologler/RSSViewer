using RSSViewer.Abstractions;

using System;
using System.Collections.Generic;
using System.Linq;

namespace RSSViewer.RssItemHandlers
{
    class ChangeStateHandler : IRssItemHandler
    {
        private readonly RssItemState _newState;

        public static string GetId(RssItemState newState)
        {
            return newState switch
            {
                RssItemState.Rejected => "477406ca-9839-4673-84d1-17987b0198e7",
                RssItemState.Undecided => "eafdaa2b-a1f8-4883-b58a-5de451e7437d",
                RssItemState.Accepted => "f385bbb8-6df2-4359-b4b7-4196cce0c4fc",
                RssItemState.Archived => "cad27543-8029-4efa-8bb7-abec9868064e",
                _ => throw new NotImplementedException()
            };
        }

        public ChangeStateHandler(RssItemState newState)
        {
            this._newState = newState;
            this.Id = GetId(newState);
            this.HandlerName = $"Change To {newState}";
        }

        public string Id { get; }

        public string HandlerName { get; }

        public bool CanbeRuleTarget => this._newState != RssItemState.Undecided;

        public IAsyncEnumerable<(IPartialRssItem, RssItemState)> HandleAsync(IReadOnlyCollection<(IPartialRssItem, RssItemState)> rssItems)
        {
            return rssItems
                .Where(z => z.Item2 != this._newState)
                .Select(z => (z.Item1, this._newState))
                .ToAsyncEnumerable();
        }
    }
}
