namespace RSSViewer.LocalDb.Helpers
{
    public class RssItemStateSnapshot
    {
        public RssItemState State { get; set; }

        public RssItemStateChangeReason StateChangeReason { get; set; }

        public string StateChangeReasonExtras { get; set; }

        public void UpdateTo(RssItem rssItem)
        {
            rssItem.State = this.State;
            rssItem.StateChangeReason = this.StateChangeReason;
            rssItem.StateChangeReasonExtras = this.StateChangeReasonExtras;
        }

        public virtual void UpdateFrom(RssItem rssItem)
        {
            this.State = rssItem.State;
            this.StateChangeReason = rssItem.StateChangeReason;
            this.StateChangeReasonExtras = rssItem.StateChangeReasonExtras;
        }
    }
}
