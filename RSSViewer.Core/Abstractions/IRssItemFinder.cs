using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSSViewer.LocalDb;

namespace RSSViewer.Abstractions
{
    interface IRssItemFinder<T>
    {
        RssItem FindRssItem(LocalDbContext context, T fromItem);
    }
}
