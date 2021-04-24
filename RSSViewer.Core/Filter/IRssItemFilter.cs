using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSSViewer.Abstractions;
using RSSViewer.Models;

namespace RSSViewer.Filter
{
    public interface IRssItemFilter
    {
        bool IsMatch(ClassifyContext<IPartialRssItem> context);
    }
}
