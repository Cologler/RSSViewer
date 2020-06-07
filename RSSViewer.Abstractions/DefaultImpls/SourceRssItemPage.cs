using RSSViewer.Abstractions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RSSViewer.DefaultImpls
{
    public class SourceRssItemPage : ISourceRssItemPage
    {
        private readonly ISourceRssItem[] _rssItems;

        public SourceRssItemPage(int? lastId, IEnumerable<ISourceRssItem> rssItems)
        {
            this.LastId = lastId;
            this._rssItems = rssItems?.ToArray() ?? throw new ArgumentNullException(nameof(rssItems));
        }

        public int? LastId { get; }

        ISourceRssItem[] ISourceRssItemPage.GetItems() => _rssItems;
    }
}
