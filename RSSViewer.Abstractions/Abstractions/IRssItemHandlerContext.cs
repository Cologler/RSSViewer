namespace RSSViewer.Abstractions
{
    public interface IRssItemHandlerContext
    {
        IPartialRssItem RssItem { get; }

        RssItemState OldState { get; set; }

        RssItemState? NewState { get; set; }
    }
}
