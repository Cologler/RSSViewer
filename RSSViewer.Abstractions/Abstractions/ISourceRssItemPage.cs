using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RSSViewer.Abstractions
{
    public interface ISourceRssItemPage
    {
        int? LastId { get; }

        ISourceRssItem[] GetItems();
    }
}
