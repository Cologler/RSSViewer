using System;
using System.Collections;
using System.Linq;

using RSSViewer.LocalDb;

namespace RSSViewer.Search
{
    internal class ContainsText : IDbSearchPart
    {
        public ContainsText(string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException($"{nameof(text)} can not be empty", nameof(text));

            this.Text = text.ToLower();
        }

        public string Text { get; }

        public IQueryable<RssItem> Where(IQueryable<RssItem> queryable)
        {
            return queryable.Where(z => z.Title.ToLower().Contains(this.Text) || z.MagnetLink.ToLower().Contains(this.Text));
        }

        public override string ToString()
        {
            return $"Contains({this.Text})";
        }
    }
}
