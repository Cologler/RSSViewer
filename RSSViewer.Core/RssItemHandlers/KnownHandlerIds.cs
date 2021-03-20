namespace RSSViewer.RssItemHandlers
{
    public static class KnownHandlerIds
    {
        /// <summary>
        /// The default handler id, for backward compatibility.
        /// Also is the id of change state to <see cref="RssItemState.Rejected"/>.
        /// </summary>
        public static readonly string DefaultHandlerId = ChangeStateHandler.GetId(RssItemState.Rejected);

        public static readonly string EmptyHandlerId = "41AA3611-CEA3-47CF-9D63-83D631A5FFF8";
    }
}
