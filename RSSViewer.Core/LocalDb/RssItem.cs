using RSSViewer.Abstractions;
using System;

namespace RSSViewer.LocalDb
{
    public class RssItem : IRssItem
    {
        public string FeedId { get; set; }

        public string RssId { get; set; }

        #region state

        public RssItemState State { get; set; }

        public RssItemStateChangeReason StateChangeReason { get; set; }

        public string StateChangeReasonExtras { get; set; }

        #endregion

        public string RawText { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Link { get; set; }

        public string MagnetLink { get; set; }

        public string GetProperty(RssItemProperties property)
        {
            switch (property)
            {
                case RssItemProperties.Title:
                    return this.Title;
                case RssItemProperties.Description:
                    return this.Description;
                case RssItemProperties.Link:
                    return this.Link;
                case RssItemProperties.MagnetLink:
                    return this.MagnetLink;

                default:
                    return string.Empty;
            }
        }

        public void UpdateFrom(ISourceRssItem sourceRssItem)
        {
            this.FeedId = sourceRssItem.FeedId;
            this.RssId = sourceRssItem.RssId;
            this.RawText = sourceRssItem.RawText;

            this.Title = sourceRssItem.GetProperty(RssItemProperties.Title);
            this.Description = sourceRssItem.GetProperty(RssItemProperties.Description);
            this.Link = sourceRssItem.GetProperty(RssItemProperties.Link);
            this.MagnetLink = sourceRssItem.GetProperty(RssItemProperties.MagnetLink);
        }

        public bool TryGetProperty(RssItemProperties property, out string value)
        {
            value = this.GetProperty(property);
            return true;
        }
    }

    public enum RssItemStateChangeReason
    {
        Unknown = 0,
        UserChoicedHandler = 1,
        MatchRuleHandler = 2
    }
}
