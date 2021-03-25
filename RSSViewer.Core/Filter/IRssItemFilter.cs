using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSSViewer.Abstractions;

namespace RSSViewer.Filter
{
    public interface IRssItemFilter
    {
        bool IsMatch(IPartialRssItem rssItem);
    }
}
