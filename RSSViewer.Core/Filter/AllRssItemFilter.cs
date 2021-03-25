
using RSSViewer.Abstractions;

namespace RSSViewer.Filter
{
    internal class AllRssItemFilter : IRssItemFilter
    {
        public bool IsMatch(IPartialRssItem rssItem) => true;
    }
}
