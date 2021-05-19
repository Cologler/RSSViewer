namespace RSSViewer.Abstractions
{
    public interface IRssItemHandlerContext
    {
        IPartialRssItem RssItem { get; }

        RssItemState OldState { get; }

        RssItemState? NewState { get; set; }
    }
}
