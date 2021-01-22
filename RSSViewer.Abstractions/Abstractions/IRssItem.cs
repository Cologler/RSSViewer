
namespace RSSViewer.Abstractions
{
    public interface IRssItem : IPartialRssItem
    {
        string Description { get; }

        string GetProperty(RssItemProperties property);
    }
}
