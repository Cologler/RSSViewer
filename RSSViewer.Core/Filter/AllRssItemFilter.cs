
using RSSViewer.Abstractions;
using RSSViewer.Models;

namespace RSSViewer.Filter
{
    class AllRssItemFilter : IRssItemFilter
    {
        public bool IsMatch(ClassifyContext<IPartialRssItem> context) => true;
    }
}
